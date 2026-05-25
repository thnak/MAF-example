using GraphRag.Pipeline.Models;

namespace GraphRag.Pipeline.Abstractions;

public interface IGraphStore
{
    Task SaveEntitiesAsync(IEnumerable<Entity> entities, CancellationToken ct = default);
    Task SaveRelationshipsAsync(IEnumerable<Relationship> relationships, CancellationToken ct = default);
    Task SaveCommunitiesAsync(IEnumerable<Community> communities, CancellationToken ct = default);
    Task SaveCommunityReportsAsync(IEnumerable<CommunityReport> reports, CancellationToken ct = default);
}
