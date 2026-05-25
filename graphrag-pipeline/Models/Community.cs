namespace GraphRag.Pipeline.Models;

public sealed class Community
{
    public required string Id { get; init; }
    public int Level { get; init; }
    public List<string> EntityIds { get; init; } = [];
    public List<string> RelationshipIds { get; init; } = [];
}
