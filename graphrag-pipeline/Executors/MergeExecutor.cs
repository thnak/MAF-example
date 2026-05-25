using GraphRag.Pipeline.Models;
using Microsoft.Agents.AI.Workflows;

namespace GraphRag.Pipeline.Executors;

// Merges extracted entities by (Name, Type) and relationships by (Source, Target).
// Duplicate descriptions are collected for later summarization.
public sealed class MergeExecutor() : Executor<ExtractionBatch, GraphData>("MergeExecutor")
{
    public override ValueTask<GraphData> HandleAsync(ExtractionBatch batch, IWorkflowContext context, CancellationToken ct = default)
    {
        var entityMap = new Dictionary<string, Entity>(StringComparer.OrdinalIgnoreCase);
        var relMap = new Dictionary<string, Relationship>(StringComparer.OrdinalIgnoreCase);

        foreach (var (_, entities, relationships) in batch.Results)
        {
            foreach (var e in entities)
            {
                var key = $"{e.Name}::{e.Type}";
                if (entityMap.TryGetValue(key, out var existing))
                {
                    existing.Descriptions.AddRange(e.Descriptions);
                    existing.TextUnitIds.AddRange(e.TextUnitIds);
                }
                else
                {
                    entityMap[key] = e;
                }
            }

            foreach (var r in relationships)
            {
                var key = $"{r.Source}→{r.Target}";
                if (relMap.TryGetValue(key, out var existing))
                {
                    existing.Descriptions.AddRange(r.Descriptions);
                    existing.TextUnitIds.AddRange(r.TextUnitIds);
                    existing.Weight += r.Weight;
                }
                else
                {
                    relMap[key] = r;
                }
            }
        }

        var graph = new GraphData
        {
            Entities = [.. entityMap.Values],
            Relationships = [.. relMap.Values],
        };

        Console.WriteLine($"[Merge] {graph.Entities.Count} unique entities, {graph.Relationships.Count} unique relationships");
        return ValueTask.FromResult(graph);
    }
}
