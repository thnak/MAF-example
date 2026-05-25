using GraphRag.Pipeline.Abstractions;
using GraphRag.Pipeline.Models;

namespace GraphRag.Pipeline.InMemory;

public sealed class InMemoryGraphStore : IGraphStore
{
    public List<Entity> Entities { get; } = [];
    public List<Relationship> Relationships { get; } = [];
    public List<Community> Communities { get; } = [];
    public List<CommunityReport> CommunityReports { get; } = [];

    public Task SaveEntitiesAsync(IEnumerable<Entity> entities, CancellationToken ct = default)
    { Entities.AddRange(entities); return Task.CompletedTask; }

    public Task SaveRelationshipsAsync(IEnumerable<Relationship> relationships, CancellationToken ct = default)
    { Relationships.AddRange(relationships); return Task.CompletedTask; }

    public Task SaveCommunitiesAsync(IEnumerable<Community> communities, CancellationToken ct = default)
    { Communities.AddRange(communities); return Task.CompletedTask; }

    public Task SaveCommunityReportsAsync(IEnumerable<CommunityReport> reports, CancellationToken ct = default)
    { CommunityReports.AddRange(reports); return Task.CompletedTask; }
}
