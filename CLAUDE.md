# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

Example projects demonstrating multi-agent workflow patterns using the **Microsoft Agent Framework (MAF)**. Each example is a standalone .NET 9 console app organized by architecture pattern.

The upstream `agent-framework/` directory is a read-only reference — do not modify it.

## Running Examples

```bash
export OPENAI_API_KEY=sk-...

# Sequential research + summarization
cd sequential/research && dotnet run

# Fan-out parallel content review
cd fanout/content-review && dotnet run

# GraphRAG indexing pipeline
cd graphrag-pipeline && dotnet run
```

No solution file exists — each subdirectory is its own independent `.csproj`.

## Key MAF Concepts

All examples reference two upstream projects via relative `ProjectReference`:
- `Microsoft.Agents.AI` — `HarnessAgent`, `WorkflowSession`
- `Microsoft.Agents.AI.Workflows` — `WorkflowBuilder`, `Executor<TIn, TOut>`, `InProcessExecution`, `IWorkflowContext`, workflow event types

### Agent patterns

| Pattern | API surface | Example |
|---------|-------------|---------|
| Sequential chain | `workflowBuilder.AddChain(a, [b, c])` | `sequential/research` |
| Fan-out (parallel) | `workflowBuilder.ForwardMessage<T>(source, [a, b, c])` | `fanout/content-review` |
| Linear executor pipeline | `new WorkflowBuilder(first).AddEdge(a, b)…Build()` | `graphrag-pipeline` |

`HarnessAgent` wraps `IChatClient` + `HarnessAgentOptions` (name, instructions, token limits).  
`Executor<TIn, TOut>` is the base class for typed pipeline stages — override `HandleAsync`.  
`InProcessExecution.RunAsync(workflow, input)` returns an async-disposable run; iterate `run.NewEvents` for `WorkflowOutputEvent`, `WorkflowErrorEvent`, and `ExecutorFailedEvent`.

## GraphRAG Pipeline Architecture

`graphrag-pipeline/` implements the Microsoft GraphRAG indexing pipeline as a linear executor chain:

```
ChunkingExecutor → ExtractionExecutor → MergeExecutor → SummarizationExecutor
    → ClusterExecutor → CommunityReportExecutor → EmbedExecutor
```

Each stage is swappable. The four core abstractions live in `Abstractions/`:
- `IGraphStore` — persist entities, relationships, communities, reports
- `IVectorStore` — store/query embedding vectors
- `IEmbeddingModel` — produce float[] embeddings
- `ICommunityDetector` — cluster entity graph into communities

`InMemory/` provides stub implementations for local development. Swap them with real backends (MongoDB, LanceDB, Leiden algorithm) for production use.

`ExtractionExecutor` uses a gleaning loop: after the initial LLM extraction pass it re-prompts up to `MaxGleanings` times asking for missed entities, then stops when the model replies `DONE`.

## Adding a New Example

1. Create `<pattern>/<name>/` with a `.csproj` targeting `net9.0`.
2. Add `ProjectReference` paths to both MAF projects (adjust `../..` depth accordingly).
3. Use `WorkflowBuilder` + `HarnessAgent` or custom `Executor<TIn, TOut>` subclasses.
4. Add a `README.md` explaining the pattern.
