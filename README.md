# MAF Examples: Multi-Agent Workflows

Advanced examples of the Microsoft Agent Framework (MAF) demonstrating real-world multi-agent patterns.

Each example is a standalone .NET project organized by **architecture pattern** and **task type**:

```
MAF-example/
├── sequential/
│   └── research/          Research → Summarization chain
├── fanout/
│   └── content-review/    Parallel multi-perspective review
└── hybrid/                (Coming soon)
    └── task/
```

## Quick Start

1. **Set your OpenAI API key:**
   ```bash
   export OPENAI_API_KEY=sk-...
   ```

2. **Run an example:**
   ```bash
   cd sequential/research
   dotnet run
   ```

## Examples

### Sequential
- **research** — Research agent gathers info, passes to summary agent for distillation

### Fan-Out
- **content-review** — One input routed to three independent review agents in parallel

### Architecture Patterns

| Pattern | Use Case | Example |
|---------|----------|---------|
| **Sequential** | Linear handoff (A → B → C) | Research → Summarize |
| **Fan-Out** | Parallel execution | Multi-reviewer consensus |
| **Hybrid** | Combine both | (Coming soon) |

## Each Example Includes

- Standalone `.csproj` with proper references to upstream agent-framework
- `Program.cs` with full working code
- `README.md` explaining the pattern and running it
- No external dependencies beyond MAF and OpenAI SDK
