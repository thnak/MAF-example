// Demonstrates LlmJsonRepair — automatic repair of malformed JSON from LLMs.
//
// Section 1 (always runs): shows the repair utility on four hardcoded patterns.
// Section 2 (requires OPENAI_API_KEY): wraps a ChatClientAgent with JsonRepairMiddleware
//   so callers receive clean JSON even when the LLM wraps its output in markdown.

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using StructuredOutput.JsonRepair;
using System.ClientModel;
using System.Text.Json;
using System.Text.Json.Serialization;

// =============================================================================
// Section 1 — static repair utility (no API key needed)
// =============================================================================

Console.WriteLine("=== LlmJsonRepair: four common malformation patterns ===\n");

ShowRepair(
    "1. Markdown code fence",
    """
    ```json
    {"city": "Hanoi", "temperature": 32, "unit": "Celsius", "condition": "sunny"}
    ```
    """);

ShowRepair(
    "2. JSON buried in prose",
    """
    Sure! Here is the weather data you requested:
    {"city": "Hanoi", "temperature": 32, "unit": "Celsius", "condition": "sunny"}
    I hope that helps. Let me know if you need anything else!
    """);

ShowRepair(
    "3. Trailing commas",
    """{"city": "Hanoi", "temperature": 32, "conditions": ["sunny", "humid",],}""");

ShowRepair(
    "4. Truncated output (token limit hit)",
    """{"city": "Hanoi", "temperature": 32, "description": "A vibrant city kn""");

static void ShowRepair(string label, string malformed)
{
    Console.WriteLine(label);
    Console.WriteLine($"  Raw:      {malformed.Trim().Replace("\n", " ").Replace("\r", "")}");
    Console.WriteLine($"  Repaired: {LlmJsonRepair.Repair(malformed)}");
    Console.WriteLine();
}

// =============================================================================
// Section 2 — agent middleware demo (requires OPENAI_API_KEY)
// =============================================================================

var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
if (apiKey is null)
{
    Console.WriteLine("[Section 2 skipped: set OPENAI_API_KEY to run the agent demo]");
    return;
}

Console.WriteLine("=== JsonRepairMiddleware with ChatClientAgent ===\n");

IChatClient chatClient = new OpenAIClient(new ApiKeyCredential(apiKey))
    .GetChatClient("gpt-4o-mini")
    .AsIChatClient();

// The agent is instructed to wrap JSON in a markdown code block and add a
// trailing explanation — this reliably produces output that fails JsonDocument.Parse.
var rawAgent = new ChatClientAgent(
    chatClient,
    instructions: """
        Always respond with a JSON object inside a markdown code block (```json ... ```).
        After the code block, add one sentence of explanation.
        """,
    name: "WeatherAgent");

// Call once without the middleware so we can print the raw LLM output
var rawResponse = await rawAgent.RunAsync(
    "What is today's weather in Hanoi? Provide: city (string), temperature (number), unit (string), condition (string).");

Console.WriteLine("Raw LLM response:");
Console.WriteLine(rawResponse.Text);
Console.WriteLine();

// Apply repair directly to show what the middleware does under the hood
var repairedText = LlmJsonRepair.Repair(rawResponse.Text);
Console.WriteLine($"After LlmJsonRepair:\n{repairedText}\n");

// Wrap the same agent with the middleware so every future call is automatically repaired
var agent = rawAgent.AsBuilder()
    .UseJsonRepair()
    .Build();

var response = await agent.RunAsync(
    "What is today's weather in Hanoi? Provide: city (string), temperature (number), unit (string), condition (string).");

try
{
    var weather = JsonSerializer.Deserialize<WeatherReport>(response.Text);
    Console.WriteLine($"Deserialized → City: {weather?.City}, Temp: {weather?.Temperature} {weather?.Unit}, Condition: {weather?.Condition}");
}
catch (JsonException ex)
{
    Console.WriteLine($"Deserialization failed (repair did not fully fix the output): {ex.Message}");
}

record WeatherReport(
    [property: JsonPropertyName("city")]        string? City,
    [property: JsonPropertyName("temperature")] int     Temperature,
    [property: JsonPropertyName("unit")]        string? Unit,
    [property: JsonPropertyName("condition")]   string? Condition);
