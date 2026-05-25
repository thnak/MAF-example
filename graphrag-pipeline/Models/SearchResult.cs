namespace GraphRag.Pipeline.Models;

public sealed class SearchResult
{
    public required string Query { get; init; }
    public required string Answer { get; init; }
    public string Context { get; init; } = "";
}
