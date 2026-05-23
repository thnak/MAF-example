# Sequential Multi-Agent Workflow: Research & Summarization

## Overview

This example demonstrates a **sequential workflow** where agents hand off work to the next one in a chain:

```
ResearchAgent (gathers info) → SummaryAgent (distills findings)
```

## Architecture

- **ResearchAgent**: Uses web search to find comprehensive information on a topic
- **SummaryAgent**: Takes the research output and creates concise key points
- **Workflow**: Linear chain connecting the two agents

## Running

```bash
export OPENAI_API_KEY=sk-...
dotnet run
```

## What This Demonstrates

1. **WorkflowBuilder** — creating a DAG of executors
2. **HarnessAgent** — two independent agents with different instructions
3. **AddChain()** — sequential handoff pattern
4. **Message forwarding** — agents receive output from prior stage
