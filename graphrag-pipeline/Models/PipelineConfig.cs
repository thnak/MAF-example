namespace GraphRag.Pipeline.Models;

public sealed class PipelineConfig
{
    public int ChunkSize { get; init; } = 150;
    public int ChunkOverlap { get; init; } = 20;
    public int MaxGleanings { get; init; } = 1;
    public int MaxParallelExtraction { get; init; } = 4;
    public string[] EntityTypes { get; init; } = ["organization", "person", "location", "event", "concept"];
}
