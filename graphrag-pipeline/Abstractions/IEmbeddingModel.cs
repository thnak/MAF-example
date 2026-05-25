namespace GraphRag.Pipeline.Abstractions;

public interface IEmbeddingModel
{
    Task<float[]> EmbedAsync(string text, CancellationToken ct = default);
}
