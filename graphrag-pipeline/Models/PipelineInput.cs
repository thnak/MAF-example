namespace GraphRag.Pipeline.Models;

public sealed class PipelineInput
{
    public required Document[] Documents { get; init; }
}
