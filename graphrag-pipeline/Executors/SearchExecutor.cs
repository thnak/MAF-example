using System.Text;
using GraphRag.Pipeline.Abstractions;
using GraphRag.Pipeline.Models;
using GraphRag.Pipeline.Prompts;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace GraphRag.Pipeline.Executors;

// Local Search pattern from GraphRAG:
//   1. Embed query → vector search for top-K entities
//   2. Expand to relationships + community reports (budget-split 50/25/25)
//   3. Build context string → LLM answer
public sealed class SearchExecutor(
    IEmbeddingModel embeddingModel,
    IVectorStore vectorStore,
    IGraphReader graphReader,
    IChatClient chatClient)
    : Executor<SearchInput, SearchResult>("SearchExecutor")
{
    private readonly AIAgent _agent = chatClient.AsAIAgent(
        instructions: "You answer questions about a knowledge graph using only the provided context.");

    public override async ValueTask<SearchResult> HandleAsync(SearchInput input, IWorkflowContext context, CancellationToken ct = default)
    {
        // 1. Embed query and find similar entities
        var queryVector = await embeddingModel.EmbedAsync(input.Query, ct);
        var hits = await vectorStore.SearchAsync(queryVector, input.TopKEntities, ct);

        var entityIds = hits.Select(h => h.Id).ToList();
        var entities = await graphReader.GetEntitiesByIdsAsync(entityIds, ct);

        // 2. Expand: relationships + community reports (50% / 25% / 25% budget split)
        var entityNames = entities.Select(e => e.Name).ToList();
        var entityIdSet = entities.Select(e => e.Id).ToList();

        var relationships = await graphReader.GetRelationshipsByEntityNamesAsync(entityNames, ct);
        var reports = await graphReader.GetCommunityReportsByEntityIdsAsync(entityIdSet, ct);

        // 3. Build context string
        var ctx = BuildContext(entities, relationships, reports);

        // 4. LLM answer
        var prompt = LocalSearchPrompts.Build(ctx, input.Query);
        var response = await _agent.RunAsync(prompt, cancellationToken: ct);

        return new SearchResult
        {
            Query = input.Query,
            Answer = response.Text,
            Context = ctx,
        };
    }

    private static string BuildContext(
        IReadOnlyList<Models.Entity> entities,
        IReadOnlyList<Relationship> relationships,
        IReadOnlyList<CommunityReport> reports)
    {
        var sb = new StringBuilder();

        if (entities.Count > 0)
        {
            sb.AppendLine("## Entities");
            foreach (var e in entities)
                sb.AppendLine($"- **{e.Name}** ({e.Type}): {e.Description}");
        }

        if (relationships.Count > 0)
        {
            sb.AppendLine("\n## Relationships");
            foreach (var r in relationships)
                sb.AppendLine($"- {r.Source} → {r.Target} (strength {r.Weight:F1}): {r.Description}");
        }

        if (reports.Count > 0)
        {
            sb.AppendLine("\n## Community Reports");
            foreach (var rpt in reports)
            {
                sb.AppendLine($"### {rpt.Title}");
                sb.AppendLine(rpt.Summary);
                foreach (var f in rpt.Findings)
                    sb.AppendLine($"- {f.Summary}: {f.Explanation}");
            }
        }

        return sb.ToString();
    }
}
