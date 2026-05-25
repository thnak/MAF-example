// Demonstrates ToolCallRepairMiddleware — automatic argument coercion and
// structured error feedback for LLM tool calls with broken arguments.
//
// Section 1 (always runs): invokes an AIFunction directly with malformed
//   arguments to show exactly what fails and how the middleware handles it.
// Section 2 (requires OPENAI_API_KEY): wraps a ChatClientAgent + tool with
//   UseToolCallRepair() so the LLM receives clear feedback when it sends
//   wrong argument types and can self-correct.

using Agents.ToolCallRepair;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;
using System.ComponentModel;
using System.Text.Json;

// =============================================================================
// Section 1 — offline coercion / error formatting demo (no API key needed)
// =============================================================================

Console.WriteLine("=== ToolCallRepairMiddleware: coercion and error formatting ===\n");

// Define a function representing an LLM tool
var forecastTool = AIFunctionFactory.Create(
    (string city, int days, bool includePollen) =>
        $"{city}: {days}-day forecast, pollen={includePollen}",
    "get_forecast",
    "Gets a weather forecast");

Console.WriteLine($"Schema: {forecastTool.JsonSchema}\n");

// Simulate how MEAI fills AIFunctionArguments from a raw LLM tool call
await ShowCoercion("string 'true' → bool",
    forecastTool,
    ("city",         JsonSerializer.SerializeToElement("Hanoi")),
    ("days",         JsonSerializer.SerializeToElement(3)),
    ("includePollen", JsonSerializer.SerializeToElement("true")));   // string, should be bool

await ShowCoercion("single-element array → scalar",
    forecastTool,
    ("city",         JsonSerializer.SerializeToElement("Hanoi")),
    ("days",         JsonSerializer.SerializeToElement(new[] { 3 })), // array, should be int
    ("includePollen", JsonSerializer.SerializeToElement(false)));

await ShowError("missing required parameter",
    forecastTool,
    ("city",  JsonSerializer.SerializeToElement("Hanoi")));         // days + includePollen missing

await ShowError("wrong container type (object for scalar)",
    forecastTool,
    ("city",         JsonSerializer.SerializeToElement("Hanoi")),
    ("days",         JsonSerializer.SerializeToElement(new { value = 3 })), // object, not coercible
    ("includePollen", JsonSerializer.SerializeToElement(false)));

// =============================================================================
// Section 2 — agent demo (requires OPENAI_API_KEY)
// =============================================================================

var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
if (apiKey is null)
{
    Console.WriteLine("[Section 2 skipped: set OPENAI_API_KEY to run the agent demo]");
    return;
}

Console.WriteLine("=== Agent demo with UseToolCallRepair() ===\n");

IChatClient chatClient = new OpenAIClient(new ApiKeyCredential(apiKey))
    .GetChatClient("gpt-4o-mini")
    .AsIChatClient();

// Register the same tool with an AITool list
var tools = new List<AITool> { forecastTool };

var agent = new ChatClientAgent(
    chatClient,
    instructions: "You are a weather assistant. Use the get_forecast tool to answer questions.",
    name: "WeatherAgent",
    tools: tools)
    .AsBuilder()
    .UseToolCallRepair()    // ← argument coercion + error feedback
    .Build();

Console.WriteLine("Query: 3-day pollen forecast for Hanoi");
var response = await agent.RunAsync("What is the 3-day forecast for Hanoi, Vietnam? Include pollen information.");
Console.WriteLine($"Response: {response.Text}\n");

// =============================================================================
// Helpers for Section 1
// =============================================================================

static async Task ShowCoercion(string label, AIFunction func, params (string Key, JsonElement Value)[] rawArgs)
{
    var args = ToArgs(rawArgs);

    Console.WriteLine($"--- {label} ---");
    PrintArgs("Before coerce", args);

    // Simulate what UseToolCallRepair does: coerce then call
    var ctx = new FunctionInvocationContext { Function = func, Arguments = args, CallContent = new FunctionCallContent("id", func.Name) };
    SimulateCoerce(ctx);

    PrintArgs("After coerce ", ctx.Arguments);

    try
    {
        var result = await func.InvokeAsync(ctx.Arguments);
        Console.WriteLine($"Result:        {result}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Still failed:  {ex.GetType().Name}: {ex.Message}");
    }

    Console.WriteLine();
}

static async Task ShowError(string label, AIFunction func, params (string Key, JsonElement Value)[] rawArgs)
{
    var args = ToArgs(rawArgs);
    Console.WriteLine($"--- {label} ---");

    try
    {
        await func.InvokeAsync(args);
    }
    catch (Exception ex)
    {
        var ctx = new FunctionInvocationContext { Function = func, Arguments = args, CallContent = new FunctionCallContent("id", func.Name) };
        var msg = FormatErrorPublic(ctx, ex);
        Console.WriteLine($"LLM receives:  {msg}");
    }

    Console.WriteLine();
}

static AIFunctionArguments ToArgs((string Key, JsonElement Value)[] pairs)
{
    var a = new AIFunctionArguments();
    foreach (var (k, v) in pairs) a[k] = v;
    return a;
}

static void PrintArgs(string tag, AIFunctionArguments args)
{
    var parts = args.Select(kv => $"{kv.Key}={kv.Value}({(kv.Value as JsonElement?)?.ValueKind})");
    Console.WriteLine($"{tag}: {string.Join(", ", parts)}");
}

// Mirror of the coerce logic for the offline demo
static void SimulateCoerce(FunctionInvocationContext ctx)
{
    if (!ctx.Function.JsonSchema.TryGetProperty("properties", out var props)) return;
    List<(string, JsonElement)>? updates = null;

    foreach (var kv in ctx.Arguments)
    {
        if (!props.TryGetProperty(kv.Key, out var paramSchema)) continue;
        if (kv.Value is not JsonElement el) continue;
        if (!paramSchema.TryGetProperty("type", out var typeEl)) continue;

        var expectedType = typeEl.GetString();
        JsonElement? coerced =
            el.ValueKind == JsonValueKind.String && expectedType == "boolean"
                ? bool.TryParse(el.GetString(), out var b) ? JsonSerializer.SerializeToElement(b) : null
            : el.ValueKind == JsonValueKind.Array
                && expectedType is "integer" or "number" or "string" or "boolean"
                && el.GetArrayLength() == 1
                ? el.EnumerateArray().First()
            : null;

        if (coerced.HasValue) (updates ??= []).Add((kv.Key, coerced.Value));
    }

    if (updates is null) return;
    foreach (var (key, val) in updates) ctx.Arguments[key] = val;
}

// Mirror of the error formatting for the offline demo
static string FormatErrorPublic(FunctionInvocationContext ctx, Exception ex)
{
    var funcName = ctx.Function.Name;

    if (ex is ArgumentException ae && ae.Message.Contains("missing a value for the required parameter"))
    {
        var start = ae.Message.IndexOf('\'');
        var end = start >= 0 ? ae.Message.IndexOf('\'', start + 1) : -1;
        var paramName = start >= 0 && end > start ? ae.Message[(start + 1)..end] : null;
        return $"ERROR calling '{funcName}': required parameter '{paramName}' was not provided. " +
               $"Retry and include all required parameters.";
    }

    if (ex is JsonException je)
        return $"ERROR calling '{funcName}': argument type mismatch — {je.Message}. " +
               $"Check the expected types and retry.";

    return $"ERROR calling '{funcName}': {ex.Message}";
}
