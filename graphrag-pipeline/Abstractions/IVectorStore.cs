namespace GraphRag.Pipeline.Abstractions;

public interface IVectorStore
{
    Task UpsertAsync(string id, float[] vector, Dictionary<string, string> metadata, CancellationToken ct = default);
    Task<VectorSearchResult[]> SearchAsync(float[] queryVector, int topK = 10, CancellationToken ct = default);
}

public sealed class VectorSearchResult
{
    public required string Id { get; init; }
    public required float Score { get; init; }
    public Dictionary<string, string> Metadata { get; init; } = [];
}
