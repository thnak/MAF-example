# JSON Repair Middleware

Automatically fixes malformed JSON from LLM responses before it reaches your deserialization code.

## Problem

LLMs frequently return JSON that fails `JsonDocument.Parse`:

| Pattern | Example |
|---------|---------|
| Markdown code fence | ` ```json\n{...}\n``` ` |
| JSON buried in prose | `"Here is the data: {...} Hope that helps!"` |
| Trailing commas | `{"key": "val", "list": [1, 2,],}` |
| Truncated output | `{"key": "incompl` (token limit hit) |

## What's in this example

| File | Purpose |
|------|---------|
| `LlmJsonRepair.cs` | Static utility — four repair passes applied in order |
| `JsonRepairMiddleware.cs` | `DelegatingAIAgent` + `UseJsonRepair()` builder extension |
| `Program.cs` | Demo: static pattern showcase + live agent with middleware |

## Repair passes

`LlmJsonRepair.Repair(string)` applies these passes in order, stopping early if the result is already valid JSON:

1. **Strip markdown fences** — removes ` ```json ... ``` ` and ` ``` ... ``` `
2. **Extract JSON content** — finds first `{`/`[`, walks to the balanced close; drops surrounding prose
3. **Remove trailing commas** — state-machine scan that skips commas before `}` or `]`, ignoring string contents
4. **Close truncated JSON** — pushes expected close chars onto a stack while scanning, appends them at the end

## Usage

```csharp
// One-off repair
string clean = LlmJsonRepair.Repair(llmText);
var obj = JsonSerializer.Deserialize<T>(clean);

// As agent middleware — repair is transparent to callers
AIAgent agent = new ChatClientAgent(chatClient, instructions: "...", name: "MyAgent")
    .AsBuilder()
    .UseJsonRepair()
    .Build();

var response = await agent.RunAsync("...");
var result = JsonSerializer.Deserialize<T>(response.Text); // always valid JSON
```

## Running

```bash
# Section 1 (static patterns) — no API key needed
dotnet run

# Section 2 (live agent demo)
export OPENAI_API_KEY=sk-...
dotnet run
```
