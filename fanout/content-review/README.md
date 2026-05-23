# Fan-Out Multi-Agent Workflow: Parallel Content Review

## Overview

This example demonstrates a **fan-out (parallel) workflow** where a single input flows to multiple agents simultaneously:

```
Content
  ├→ QualityReviewer (checks clarity & accuracy)
  ├→ ComplianceReviewer (checks security & regulations)
  └→ StyleReviewer (checks tone & consistency)
```

All three reviewers operate in parallel and return their findings.

## Architecture

- **QualityReviewer**: Evaluates technical clarity and correctness
- **ComplianceReviewer**: Checks for security/privacy/regulatory issues
- **StyleReviewer**: Assesses tone, voice, and writing style
- **Workflow**: Fan-out pattern using `ForwardMessage<T>`

## Running

```bash
export OPENAI_API_KEY=sk-...
dotnet run
```

## What This Demonstrates

1. **ForwardMessage<T>()** — routing messages to multiple targets
2. **Parallel execution** — multiple agents work independently
3. **Message type filtering** — routing based on message type
4. **Workflow composition** — connecting independent agents without sequential dependencies
