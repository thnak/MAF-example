using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using System.Text.Json;

namespace Agents.ToolCallRepair;

/// <summary>
/// Intercepts each AIFunction invocation to:
///   1. Coerce common argument mismatches before calling the function
///      (e.g. string "true" → bool, single-element array → scalar)
///   2. Format exceptions into structured messages so the LLM receives
///      actionable feedback and can retry with corrected arguments
/// </summary>
public static class ToolCallRepairAgentBuilderExtensions
{
    /// <summary>
    /// Adds tool-call argument repair to the agent pipeline.
    /// Requires the inner agent to have a FunctionInvokingChatClient in its chain.
    /// </summary>
    public static AIAgentBuilder UseToolCallRepair(this AIAgentBuilder builder)
        => builder.Use(async (agent, context, next, ct) =>
        {
            TryCoerceArguments(context);
            RepairSkillScriptArguments(context);

            try
            {
                return await next(context, ct);
            }
            catch (Exception ex)
            {
                return FormatError(context, ex);
            }
        });

    // -------------------------------------------------------------------------
    // Coercion: fix common type mismatches before the function sees the args
    // -------------------------------------------------------------------------

    private static void TryCoerceArguments(FunctionInvocationContext context)
    {
        if (!context.Function.JsonSchema.TryGetProperty("properties", out var properties))
            return;

        List<(string Key, JsonElement Value)>? updates = null;

        foreach (var kv in context.Arguments)
        {
            if (!properties.TryGetProperty(kv.Key, out var paramSchema)) continue;
            if (kv.Value is not JsonElement element) continue;
            if (!paramSchema.TryGetProperty("type", out var typeEl)) continue;

            var coerced = TryCoerce(element, typeEl.GetString());
            if (coerced is { } value)
                (updates ??= []).Add((kv.Key, value));
        }

        if (updates is null) return;

        foreach (var (key, value) in updates)
            context.Arguments[key] = value;
    }

    private static JsonElement? TryCoerce(JsonElement value, string? expectedType) =>
        // JSON string "true"/"false" → boolean
        value.ValueKind == JsonValueKind.String && expectedType == "boolean"
            ? bool.TryParse(value.GetString(), out var b)
                ? JsonSerializer.SerializeToElement(b)
                : null

        // JSON array [x] → unwrap first element for any scalar type
        : value.ValueKind == JsonValueKind.Array
            && expectedType is "integer" or "number" or "string" or "boolean"
            && value.GetArrayLength() == 1
            ? value.EnumerateArray().First()

        : null;

    // AgentSkillsProvider's run_skill_script passes the inner script's arguments as a
    // JsonElement? parameter named "arguments". ConvertToFunctionArguments (MAF internal)
    // throws InvalidOperationException when that element is not a JSON object — which
    // happens when the LLM omits or misformats arguments for a parameterless script.
    //
    // Fix: coerce the "arguments" value to null (→ empty AIFunctionArguments) or, when
    // the LLM JSON-stringified the args object, unwrap the double-encoded string.
    private static void RepairSkillScriptArguments(FunctionInvocationContext context)
    {
        if (context.Function.Name != "run_skill_script") return;
        if (!context.Arguments.TryGetValue("arguments", out var raw)) return;
        if (raw is not JsonElement el) return;

        // Already valid or already null/undefined — nothing to do.
        if (el.ValueKind is JsonValueKind.Object or JsonValueKind.Null or JsonValueKind.Undefined)
            return;

        // LLM sometimes JSON-stringifies the args object: '"{\"key\":1}"' → try to unwrap.
        if (el.ValueKind == JsonValueKind.String)
        {
            var s = el.GetString();
            if (!string.IsNullOrEmpty(s))
            {
                try
                {
                    using var doc = JsonDocument.Parse(s);
                    if (doc.RootElement.ValueKind == JsonValueKind.Object)
                    {
                        context.Arguments["arguments"] = doc.RootElement.Clone();
                        return;
                    }
                }
                catch (JsonException) { /* fall through to null */ }
            }
        }

        // Any other non-object value (empty string, number, array, …) → treat as no args.
        context.Arguments["arguments"] = null;
    }

    // -------------------------------------------------------------------------
    // Error formatting: turn exceptions into LLM-readable feedback
    // -------------------------------------------------------------------------

    private static string FormatError(FunctionInvocationContext context, Exception ex)
    {
        var funcName = context.Function.Name;

        // Missing required parameter
        if (ex is ArgumentException ae && ae.Message.Contains("missing a value for the required parameter"))
        {
            var paramName = ExtractParamName(ae.Message);
            var schema = context.Function.JsonSchema;
            var expectedType = GetExpectedType(schema, paramName);
            return $"ERROR calling '{funcName}': required parameter '{paramName}' ({expectedType}) was not provided. " +
                   $"Retry the tool call and include all required parameters.";
        }

        // Type conversion failure
        if (ex is JsonException je)
        {
            var badParam = FindBadParam(context, je);
            return $"ERROR calling '{funcName}': argument type mismatch — {je.Message}. " +
                   $"Check the expected types in the tool schema and retry." +
                   (badParam is not null ? $" (problematic parameter: '{badParam}')" : "");
        }

        return $"ERROR calling '{funcName}': {ex.Message}";
    }

    private static string? ExtractParamName(string message)
    {
        // "...required parameter 'name'..."
        var start = message.IndexOf('\'');
        var end = start >= 0 ? message.IndexOf('\'', start + 1) : -1;
        return start >= 0 && end > start ? message[(start + 1)..end] : null;
    }

    private static string GetExpectedType(JsonElement schema, string? paramName)
    {
        if (paramName is null) return "unknown";
        if (schema.TryGetProperty("properties", out var props) &&
            props.TryGetProperty(paramName, out var paramSchema) &&
            paramSchema.TryGetProperty("type", out var typeEl))
            return typeEl.GetString() ?? "unknown";
        return "unknown";
    }

    private static string? FindBadParam(FunctionInvocationContext context, JsonException je)
    {
        // JsonException.Path contains something like "$.paramName" — extract it
        var path = je.Path;
        if (path is null or "$") return null;
        return path.StartsWith("$.") ? path[2..] : path;
    }
}
