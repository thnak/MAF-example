namespace GraphRag.Pipeline.Models;

public sealed class TextUnit
{
    public required string Id { get; init; }
    public required string Text { get; init; }
    public required string DocumentId { get; init; }
    public int ChunkIndex { get; init; }
}
