namespace GraphRag.Pipeline.Models;

public sealed class ExtractionBatch
{
    public List<(string TextUnitId, List<Entity> Entities, List<Relationship> Relationships)> Results { get; init; } = [];
}
