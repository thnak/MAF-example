using GraphRag.Pipeline.Abstractions;

namespace GraphRag.Pipeline.InMemory;

// Returns random normalized vectors. Replace with a real embeddings client (e.g. OpenAI text-embedding-3-small).
public sealed class StubEmbeddingModel : IEmbeddingModel
{
    private readonly Random _rng = new(42);

    public Task<float[]> EmbedAsync(string text, CancellationToken ct = default)
    {
        var vector = new float[1536];
        for (int i = 0; i < vector.Length; i++)
            vector[i] = (float)(_rng.NextDouble() * 2 - 1);

        float norm = MathF.Sqrt(vector.Sum(x => x * x));
        for (int i = 0; i < vector.Length; i++)
            vector[i] /= norm;

        return Task.FromResult(vector);
    }
}
