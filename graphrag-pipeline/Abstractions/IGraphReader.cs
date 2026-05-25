using GraphRag.Pipeline.Models;

namespace GraphRag.Pipeline.Abstractions;

public interface IGraphReader
{
    Task<IReadOnlyList<Entity>> GetEntitiesByIdsAsync(IEnumerable<string> ids, CancellationToken ct = default);
    Task<IReadOnlyList<Relationship>> GetRelationshipsByEntityNamesAsync(IEnumerable<string> entityNames, CancellationToken ct = default);
    Task<IReadOnlyList<CommunityReport>> GetCommunityReportsByEntityIdsAsync(IEnumerable<string> entityIds, CancellationToken ct = default);
}
