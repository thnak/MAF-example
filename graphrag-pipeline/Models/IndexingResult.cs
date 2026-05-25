namespace GraphRag.Pipeline.Models;

public sealed class IndexingResult
{
    public int EntityCount { get; init; }
    public int RelationshipCount { get; init; }
    public int CommunityCount { get; init; }
    public int VectorCount { get; init; }
    public TimeSpan Elapsed { get; init; }
}
