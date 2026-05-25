# Tool Call Repair Middleware

Automatically fixes broken tool call arguments from LLMs and gives structured error feedback so the model can self-correct.

## Problem

LLMs frequently send tool call arguments that fail deserialization:

| Failure | Example | Error |
|---------|---------|-------|
| String for bool | `"includePollen": "true"` | `JsonException: cannot convert String to Boolean` |
| Array for scalar | `"days": [3]` | `JsonException: cannot convert Array to Int32` |
| Missing required | `{}` — omits required `city` | `ArgumentException: missing required parameter 'city'` |
| Unconvertible | `"days": {"value": 3}` | `JsonException: cannot convert Object to Int32` |

Note: MEAI already handles `"5"` → `int 5` (string numbers) via `JsonNumberHandling.AllowReadingFromString`, so that case is not a problem.

## What's in this example

| File | Purpose |
|------|---------|
| `ToolCallRepairMiddleware.cs` | Intercepts each function invocation: coerces args before the call, formats exceptions after |
| `Program.cs` | Section 1 offline demo (no key needed) + Section 2 live agent demo |

## How the middleware works

The middleware hooks into `AIAgentBuilder.Use(callback)` which runs inside the MEAI `FunctionInvokingChatClient` tool-call loop:

```
LLM ──► FunctionCallContent ──► [UseToolCallRepair callback]
                                       │
                                  1. TryCoerceArguments
                                       │ coerces string→bool, [x]→x
                                       │
                                  2. next(context, ct)  ← calls the actual function
                                       │
                                  3. catch(Exception)
                                       │ returns formatted error string
                                       ▼
                              FunctionResultContent ──► LLM (can now self-correct)
```

### Coercions applied

| Actual value | Expected type | Coerced to |
|---|---|---|
| `"true"` / `"false"` (JSON string) | `boolean` | `true` / `false` |
| `[x]` (single-element array) | `integer`, `number`, `string`, `boolean` | `x` |

### Error messages returned to LLM

```
ERROR calling 'get_forecast': required parameter 'days' (integer) was not provided.
  Retry the tool call and include all required parameters.

ERROR calling 'get_forecast': argument type mismatch — The JSON value could not be
  converted to System.Int32. Check the expected types in the tool schema and retry.
  (problematic parameter: 'days')
```

## Usage

```csharp
var tools = new List<AITool> { myTool };

var agent = new ChatClientAgent(chatClient, instructions: "...", name: "Agent", tools: tools)
    .AsBuilder()
    .UseToolCallRepair()   // ← add this
    .Build();
```

## Running

```bash
# Section 1 — offline coercion demo, no key needed
dotnet run

# Section 2 — live agent demo
export OPENAI_API_KEY=sk-...
dotnet run
```

## Limitations

- Cannot coerce `object` or `array` to a scalar (returns error to LLM instead)
- Cannot supply a missing required parameter — returns error message so the LLM can retry
- Does not retry the full agent turn; relies on `FunctionInvokingChatClient`'s built-in retry loop (the LLM sees the error result and can call the tool again)
