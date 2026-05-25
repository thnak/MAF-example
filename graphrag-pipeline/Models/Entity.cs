namespace GraphRag.Pipeline.Models;

public sealed class Entity
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Type { get; init; }
    public string Description { get; set; } = "";
    public List<string> Descriptions { get; init; } = [];
    public List<string> TextUnitIds { get; init; } = [];
}
