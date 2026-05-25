namespace GraphRag.Pipeline.Models;

public sealed class CommunityReport
{
    public required string CommunityId { get; init; }
    public string Title { get; set; } = "";
    public string Summary { get; set; } = "";
    public List<Finding> Findings { get; init; } = [];
}

public sealed class Finding
{
    public string Summary { get; init; } = "";
    public string Explanation { get; init; } = "";
}
