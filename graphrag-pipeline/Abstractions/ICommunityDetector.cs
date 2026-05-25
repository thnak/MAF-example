using GraphRag.Pipeline.Models;

namespace GraphRag.Pipeline.Abstractions;

public interface ICommunityDetector
{
    Task<Community[]> DetectAsync(IEnumerable<Entity> entities, IEnumerable<Relationship> relationships, CancellationToken ct = default);
}
