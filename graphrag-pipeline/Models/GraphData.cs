namespace GraphRag.Pipeline.Models;

public sealed class GraphData
{
    public List<Entity> Entities { get; init; } = [];
    public List<Relationship> Relationships { get; init; } = [];
}
