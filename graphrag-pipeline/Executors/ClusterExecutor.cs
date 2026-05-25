using GraphRag.Pipeline.Abstractions;
using GraphRag.Pipeline.Models;
using Microsoft.Agents.AI.Workflows;

namespace GraphRag.Pipeline.Executors;

// Delegates community detection to ICommunityDetector.
// Swap the implementation to use Leiden, Louvain, or any graph clustering algorithm.
public sealed class ClusterExecutor(ICommunityDetector detector) : Executor<GraphData, ClusteredGraph>("ClusterExecutor")
{
    public override async ValueTask<ClusteredGraph> HandleAsync(GraphData graph, IWorkflowContext context, CancellationToken ct = default)
    {
        var communities = await detector.DetectAsync(graph.Entities, graph.Relationships, ct);
        Console.WriteLine($"[Clustering] {communities.Length} communities detected");
        return new ClusteredGraph { Graph = graph, Communities = [.. communities] };
    }
}
