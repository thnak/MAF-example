using GraphRag.Pipeline.Models;
using GraphRag.Pipeline.Prompts;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace GraphRag.Pipeline.Executors;

// For each entity with more than one description, calls the LLM to produce
// a single consolidated description. Single-description entities pass through unchanged.
public sealed class SummarizationExecutor(IChatClient chatClient) : Executor<GraphData, GraphData>("SummarizationExecutor")
{
    private readonly AIAgent _agent = chatClient.AsAIAgent(
        instructions: "You summarize entity descriptions into one comprehensive description. Output only the summary text.");

    public override async ValueTask<GraphData> HandleAsync(GraphData graph, IWorkflowContext context, CancellationToken ct = default)
    {
        int summarized = 0;

        foreach (var entity in graph.Entities)
        {
            if (entity.Descriptions.Count > 1)
            {
                var prompt = SummarizationPrompts.Build(entity.Name, entity.Descriptions);
                var response = await _agent.RunAsync(prompt, cancellationToken: ct);
                entity.Description = response.Text;
                summarized++;
            }
            else
            {
                entity.Description = entity.Descriptions.FirstOrDefault() ?? "";
            }
        }

        foreach (var rel in graph.Relationships)
            rel.Description = rel.Descriptions.FirstOrDefault() ?? "";

        Console.WriteLine($"[Summarization] {summarized} entities summarized");
        return graph;
    }
}
