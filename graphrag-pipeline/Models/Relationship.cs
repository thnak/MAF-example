namespace GraphRag.Pipeline.Models;

public sealed class Relationship
{
    public required string Id { get; init; }
    public required string Source { get; init; }
    public required string Target { get; init; }
    public string Description { get; set; } = "";
    public List<string> Descriptions { get; init; } = [];
    public float Weight { get; set; } = 1.0f;
    public List<string> TextUnitIds { get; init; } = [];
}
