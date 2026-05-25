namespace GraphRag.Pipeline.Models;

public sealed class ClusteredGraph
{
    public required GraphData Graph { get; init; }
    public List<Community> Communities { get; init; } = [];
    public List<CommunityReport> CommunityReports { get; set; } = [];
}
