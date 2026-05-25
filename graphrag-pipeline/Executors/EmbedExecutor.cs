using GraphRag.Pipeline.Abstractions;
using GraphRag.Pipeline.Models;
using Microsoft.Agents.AI.Workflows;

namespace GraphRag.Pipeline.Executors;

// Persists the final graph to IGraphStore and embeds entity descriptions into IVectorStore.
// Swap IEmbeddingModel for a real OpenAI/Azure embeddings client in production.
public sealed class EmbedExecutor(IEmbeddingModel embedder, IVectorStore vectors, IGraphStore graphStore)
    : Executor<ClusteredGraph, IndexingResult>("EmbedExecutor")
{
    public override async ValueTask<IndexingResult> HandleAsync(ClusteredGraph clustered, IWorkflowContext context, CancellationToken ct = default)
    {
        var start = DateTime.UtcNow;
        var graph = clustered.Graph;

        await graphStore.SaveEntitiesAsync(graph.Entities, ct);
        await graphStore.SaveRelationshipsAsync(graph.Relationships, ct);
        await graphStore.SaveCommunitiesAsync(clustered.Communities, ct);
        await graphStore.SaveCommunityReportsAsync(clustered.CommunityReports, ct);

        int vectorCount = 0;
        foreach (var entity in graph.Entities.Where(e => !string.IsNullOrWhiteSpace(e.Description)))
        {
            var embedding = await embedder.EmbedAsync(entity.Description, ct);
            await vectors.UpsertAsync(entity.Id, embedding, new Dictionary<string, string>
            {
                ["name"] = entity.Name,
                ["type"] = entity.Type,
                ["kind"] = "entity",
            }, ct);
            vectorCount++;
        }

        Console.WriteLine($"[Embed] {vectorCount} vectors stored");

        return new IndexingResult
        {
            EntityCount = graph.Entities.Count,
            RelationshipCount = graph.Relationships.Count,
            CommunityCount = clustered.Communities.Count,
            VectorCount = vectorCount,
            Elapsed = DateTime.UtcNow - start,
        };
    }
}
