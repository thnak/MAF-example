using GraphRag.Pipeline.Abstractions;
using GraphRag.Pipeline.Models;

namespace GraphRag.Pipeline.InMemory;

// Read-side view over InMemoryGraphStore. Pass the same store instance to both.
public sealed class InMemoryGraphReader(InMemoryGraphStore store) : IGraphReader
{
    public Task<IReadOnlyList<Entity>> GetEntitiesByIdsAsync(IEnumerable<string> ids, CancellationToken ct = default)
    {
        var set = ids.ToHashSet();
        IReadOnlyList<Entity> result = store.Entities.Where(e => set.Contains(e.Id)).ToList();
        return Task.FromResult(result);
    }

    public Task<IReadOnlyList<Relationship>> GetRelationshipsByEntityNamesAsync(IEnumerable<string> entityNames, CancellationToken ct = default)
    {
        var names = entityNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
        IReadOnlyList<Relationship> result = store.Relationships
            .Where(r => names.Contains(r.Source) || names.Contains(r.Target))
            .ToList();
        return Task.FromResult(result);
    }

    public Task<IReadOnlyList<CommunityReport>> GetCommunityReportsByEntityIdsAsync(IEnumerable<string> entityIds, CancellationToken ct = default)
    {
        var ids = entityIds.ToHashSet();
        // Communities that overlap with the matched entity set
        var communityIds = store.Communities
            .Where(c => c.EntityIds.Any(ids.Contains))
            .Select(c => c.Id)
            .ToHashSet();

        IReadOnlyList<CommunityReport> result = store.CommunityReports
            .Where(r => communityIds.Contains(r.CommunityId))
            .ToList();
        return Task.FromResult(result);
    }
}
