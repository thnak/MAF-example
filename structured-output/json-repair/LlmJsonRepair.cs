using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace StructuredOutput.JsonRepair;

/// <summary>
/// Heuristic fixer for malformed JSON commonly produced by LLMs.
/// Handles four patterns: markdown code fences, surrounding prose,
/// trailing commas, and truncated output.
/// </summary>
public static partial class LlmJsonRepair
{
    public static string Repair(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;

        // Strip BOM and null bytes that some LLM providers occasionally emit
        var text = input.TrimStart('﻿').Replace("\0", "");

        if (IsValidJson(text.Trim()))
            return text.Trim();

        text = StripMarkdownFences(text);

        if (IsValidJson(text.Trim()))
            return text.Trim();

        text = ExtractJsonContent(text);
        text = RemoveTrailingCommas(text);

        if (!IsValidJson(text))
            text = CloseTruncated(text);

        return text.Trim();
    }

    // -------------------------------------------------------------------------
    // Pass 1: strip ```json ... ``` and ``` ... ``` fences
    // -------------------------------------------------------------------------

    private static string StripMarkdownFences(string text)
    {
        var match = FencePattern().Match(text);
        return match.Success ? match.Groups[1].Value.Trim() : text.Trim();
    }

    [GeneratedRegex(@"```(?:json|JSON)?\s*\r?\n?([\s\S]*?)\r?\n?```", RegexOptions.Singleline)]
    private static partial Regex FencePattern();

    // -------------------------------------------------------------------------
    // Pass 2: extract the first balanced JSON object/array from surrounding prose
    // -------------------------------------------------------------------------

    private static string ExtractJsonContent(string text)
    {
        text = text.Trim();

        int start = -1;
        char open = '{', close = '}';

        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '{' || text[i] == '[')
            {
                start = i;
                open = text[i];
                close = open == '{' ? '}' : ']';
                break;
            }
        }

        if (start < 0)
            return text;

        int depth = 0;
        bool inString = false;
        bool escaped = false;

        for (int i = start; i < text.Length; i++)
        {
            char c = text[i];

            if (escaped) { escaped = false; continue; }
            if (c == '\\' && inString) { escaped = true; continue; }
            if (c == '"') { inString = !inString; continue; }
            if (inString) continue;

            if (c == open) depth++;
            else if (c == close)
            {
                depth--;
                if (depth == 0)
                    return text[start..(i + 1)];
            }
        }

        // No matching close found — return from start (CloseTruncated will fix it)
        return text[start..];
    }

    // -------------------------------------------------------------------------
    // Pass 3: remove trailing commas before } or ] (e.g. {"a":1,} or [1,2,])
    // Uses a state machine to avoid touching commas inside string values.
    // -------------------------------------------------------------------------

    private static string RemoveTrailingCommas(string text)
    {
        var sb = new StringBuilder(text.Length);
        bool inString = false;
        bool escaped = false;

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];

            if (escaped) { sb.Append(c); escaped = false; continue; }
            if (c == '\\' && inString) { sb.Append(c); escaped = true; continue; }
            if (c == '"') { inString = !inString; sb.Append(c); continue; }

            if (c == ',' && !inString)
            {
                // Look ahead past whitespace; skip comma if next token is } or ]
                int j = i + 1;
                while (j < text.Length && char.IsWhiteSpace(text[j])) j++;
                if (j < text.Length && (text[j] == '}' || text[j] == ']'))
                    continue;
            }

            sb.Append(c);
        }

        return sb.ToString();
    }

    // -------------------------------------------------------------------------
    // Pass 4: close any unclosed braces/brackets from a truncated response
    // -------------------------------------------------------------------------

    private static string CloseTruncated(string text)
    {
        var stack = new Stack<char>();
        bool inString = false;
        bool escaped = false;

        foreach (char c in text)
        {
            if (escaped) { escaped = false; continue; }
            if (c == '\\' && inString) { escaped = true; continue; }
            if (c == '"') { inString = !inString; continue; }
            if (inString) continue;

            if (c == '{') stack.Push('}');
            else if (c == '[') stack.Push(']');
            else if ((c == '}' || c == ']') && stack.Count > 0 && stack.Peek() == c)
                stack.Pop();
        }

        var sb = new StringBuilder(text);

        // Close an unterminated string first
        if (inString) sb.Append('"');

        while (stack.Count > 0)
            sb.Append(stack.Pop());

        return sb.ToString();
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static bool IsValidJson(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        try { JsonDocument.Parse(text); return true; }
        catch (JsonException) { return false; }
    }
}
