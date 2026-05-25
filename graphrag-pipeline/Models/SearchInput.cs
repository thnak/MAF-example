namespace GraphRag.Pipeline.Models;

public sealed class SearchInput
{
    public required string Query { get; init; }
    public int TopKEntities { get; init; } = 10;
}
