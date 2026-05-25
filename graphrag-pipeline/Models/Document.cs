namespace GraphRag.Pipeline.Models;

public sealed class Document
{
    public required string Id { get; init; }
    public required string Text { get; init; }
    public string? Title { get; init; }
}
