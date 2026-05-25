using GraphRag.Pipeline.Abstractions;

namespace GraphRag.Pipeline.InMemory;

public sealed class InMemoryVectorStore : IVectorStore
{
    private readonly List<(string Id, float[] Vector, Dictionary<string, string> Metadata)> _entries = [];

    public Task UpsertAsync(string id, float[] vector, Dictionary<string, string> metadata, CancellationToken ct = default)
    {
        _entries.RemoveAll(x => x.Id == id);
        _entries.Add((id, vector, metadata));
        return Task.CompletedTask;
    }

    public Task<VectorSearchResult[]> SearchAsync(float[] queryVector, int topK = 10, CancellationToken ct = default)
    {
        var results = _entries
            .Select(x => new VectorSearchResult
            {
                Id = x.Id,
                Score = CosineSimilarity(queryVector, x.Vector),
                Metadata = x.Metadata,
            })
            .OrderByDescending(x => x.Score)
            .Take(topK)
            .ToArray();

        return Task.FromResult(results);
    }

    private static float CosineSimilarity(float[] a, float[] b)
    {
        if (a.Length != b.Length) return 0f;
        float dot = 0f, normA = 0f, normB = 0f;
        for (int i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }
        return normA == 0f || normB == 0f ? 0f : dot / (MathF.Sqrt(normA) * MathF.Sqrt(normB));
    }
}
