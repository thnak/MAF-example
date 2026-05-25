using System.Text;
using System.Text.Json;
using GraphRag.Pipeline.Models;
using GraphRag.Pipeline.Prompts;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace GraphRag.Pipeline.Executors;

// Generates an LLM summary report for each detected community.
// Reports are structured JSON with title, summary, and findings.
public sealed class CommunityReportExecutor(IChatClient chatClient) : Executor<ClusteredGraph, ClusteredGraph>("CommunityReportExecutor")
{
    private readonly AIAgent _agent = chatClient.AsAIAgent(
        instructions: "You generate structured community analysis reports as valid JSON.");

    public override async ValueTask<ClusteredGraph> HandleAsync(ClusteredGraph clustered, IWorkflowContext context, CancellationToken ct = default)
    {
        var reports = new List<CommunityReport>();

        foreach (var community in clustered.Communities)
        {
            var contextData = BuildContext(community, clustered.Graph);
            var prompt = CommunityReportPrompts.Build(contextData);
            var response = await _agent.RunAsync(prompt, cancellationToken: ct);
            reports.Add(ParseReport(community.Id, response.Text));
        }

        clustered.CommunityReports = reports;
        Console.WriteLine($"[CommunityReports] {reports.Count} reports generated");
        return clustered;
    }

    private static string BuildContext(Community community, GraphData graph)
    {
        var entityById = graph.Entities.ToDictionary(e => e.Id);
        var relById = graph.Relationships.ToDictionary(r => r.Id);

        var sb = new StringBuilder();
        sb.AppendLine("Entities:");
        foreach (var id in community.EntityIds)
            if (entityById.TryGetValue(id, out var e))
                sb.AppendLine($"- {e.Name} ({e.Type}): {e.Description}");

        sb.AppendLine("\nRelationships:");
        foreach (var id in community.RelationshipIds)
            if (relById.TryGetValue(id, out var r))
                sb.AppendLine($"- {r.Source} → {r.Target} (strength {r.Weight:F1}): {r.Description}");

        return sb.ToString();
    }

    private static CommunityReport ParseReport(string communityId, string json)
    {
        try
        {
            var start = json.IndexOf('{');
            var end = json.LastIndexOf('}');
            if (start >= 0 && end > start)
                json = json[start..(end + 1)];

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var report = new CommunityReport
            {
                CommunityId = communityId,
                Title = root.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "",
                Summary = root.TryGetProperty("summary", out var s) ? s.GetString() ?? "" : "",
            };

            if (root.TryGetProperty("findings", out var findings))
                foreach (var f in findings.EnumerateArray())
                    report.Findings.Add(new Finding
                    {
                        Summary = f.TryGetProperty("summary", out var fs) ? fs.GetString() ?? "" : "",
                        Explanation = f.TryGetProperty("explanation", out var fe) ? fe.GetString() ?? "" : "",
                    });

            return report;
        }
        catch
        {
            return new CommunityReport { CommunityId = communityId, Title = "Community Report", Summary = json };
        }
    }
}
