using GraphRag.Pipeline.Models;
using Microsoft.Agents.AI.Workflows;

namespace GraphRag.Pipeline.Executors;

public sealed class ChunkingExecutor(PipelineConfig config) : Executor<PipelineInput, TextUnit[]>("ChunkingExecutor")
{
    public override ValueTask<TextUnit[]> HandleAsync(PipelineInput input, IWorkflowContext context, CancellationToken ct = default)
    {
        var units = new List<TextUnit>();
        foreach (var doc in input.Documents)
        {
            var chunks = ChunkText(doc.Text, config.ChunkSize, config.ChunkOverlap);
            for (int i = 0; i < chunks.Count; i++)
            {
                units.Add(new TextUnit
                {
                    Id = $"{doc.Id}-chunk-{i}",
                    Text = chunks[i],
                    DocumentId = doc.Id,
                    ChunkIndex = i,
                });
            }
        }

        Console.WriteLine($"[Chunking] {input.Documents.Length} documents → {units.Count} text units");
        return ValueTask.FromResult(units.ToArray());
    }

    private static List<string> ChunkText(string text, int chunkSize, int overlap)
    {
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var chunks = new List<string>();
        int start = 0;
        while (start < words.Length)
        {
            int end = Math.Min(start + chunkSize, words.Length);
            chunks.Add(string.Join(" ", words[start..end]));
            if (end == words.Length) break;
            start += chunkSize - overlap;
        }
        return chunks;
    }
}
