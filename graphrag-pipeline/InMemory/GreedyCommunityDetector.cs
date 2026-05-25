using GraphRag.Pipeline.Abstractions;
using GraphRag.Pipeline.Models;

namespace GraphRag.Pipeline.InMemory;

// Connected-component community detection via Union-Find.
// Replace with Leiden or Louvain for production-quality clustering.
public sealed class GreedyCommunityDetector : ICommunityDetector
{
    public Task<Community[]> DetectAsync(IEnumerable<Entity> entities, IEnumerable<Relationship> relationships, CancellationToken ct = default)
    {
        var entityList = entities.ToList();
        var relList = relationships.ToList();

        var parent = entityList.ToDictionary(e => e.Name, e => e.Name, StringComparer.OrdinalIgnoreCase);

        string Find(string x)
        {
            if (!parent.ContainsKey(x)) parent[x] = x;
            if (!string.Equals(parent[x], x, StringComparison.OrdinalIgnoreCase))
                parent[x] = Find(parent[x]);
            return parent[x];
        }

        void Union(string x, string y)
        {
            var px = Find(x);
            var py = Find(y);
            if (!string.Equals(px, py, StringComparison.OrdinalIgnoreCase))
                parent[px] = py;
        }

        foreach (var rel in relList)
            Union(rel.Source, rel.Target);

        var communities = entityList
            .GroupBy(e => Find(e.Name), StringComparer.OrdinalIgnoreCase)
            .Select((group, i) =>
            {
                var groupEntityNames = group.Select(e => e.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
                return new Community
                {
                    Id = $"community-{i}",
                    Level = 0,
                    EntityIds = [.. group.Select(e => e.Id)],
                    RelationshipIds = [.. relList
                        .Where(r => groupEntityNames.Contains(r.Source) || groupEntityNames.Contains(r.Target))
                        .Select(r => r.Id)],
                };
            })
            .ToArray();

        return Task.FromResult(communities);
    }
}
