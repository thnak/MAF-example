using GraphRag.Pipeline.Models;
using GraphRag.Pipeline.Prompts;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace GraphRag.Pipeline.Executors;

// LLM-based entity and relationship extraction with optional gleaning loop.
// Each text unit is processed in parallel up to MaxParallelExtraction.
// Gleanings re-prompt the LLM asking for missed entities (up to MaxGleanings times).
public sealed class ExtractionExecutor(IChatClient chatClient, PipelineConfig config)
    : Executor<TextUnit[], ExtractionBatch>("ExtractionExecutor")
{
    public override async ValueTask<ExtractionBatch> HandleAsync(TextUnit[] units, IWorkflowContext context, CancellationToken ct = default)
    {
        var batch = new ExtractionBatch();
        var semaphore = new SemaphoreSlim(config.MaxParallelExtraction, config.MaxParallelExtraction);

        var tasks = units.Select(async unit =>
        {
            await semaphore.WaitAsync(ct);
            try { return await ExtractFromUnitAsync(unit, ct); }
            finally { semaphore.Release(); }
        });

        foreach (var (unitId, entities, relationships) in await Task.WhenAll(tasks))
            batch.Results.Add((unitId, entities, relationships));

        int totalEntities = batch.Results.Sum(r => r.Entities.Count);
        int totalRelationships = batch.Results.Sum(r => r.Relationships.Count);
        Console.WriteLine($"[Extraction] {units.Length} units → {totalEntities} entities, {totalRelationships} relationships");
        return batch;
    }

    private async Task<(string UnitId, List<Entity> Entities, List<Relationship> Relationships)> ExtractFromUnitAsync(TextUnit unit, CancellationToken ct)
    {
        var prompt = ExtractionPrompts.BuildExtractionPrompt(unit.Text, config.EntityTypes);
        var messages = new List<ChatMessage> { new(ChatRole.User, prompt) };

        var completion = await chatClient.GetResponseAsync(messages, cancellationToken: ct);
        var responseText = completion.Text ?? "";
        messages.Add(new ChatMessage(ChatRole.Assistant, responseText));

        for (int i = 0; i < config.MaxGleanings; i++)
        {
            messages.Add(new ChatMessage(ChatRole.User, ExtractionPrompts.GleaningPrompt));
            var gleaning = await chatClient.GetResponseAsync(messages, cancellationToken: ct);
            var gleaningText = gleaning.Text ?? "";
            if (gleaningText.Contains("DONE", StringComparison.OrdinalIgnoreCase)) break;
            responseText += ExtractionPrompts.RecordDelimiter + gleaningText;
            messages.Add(new ChatMessage(ChatRole.Assistant, gleaningText));
        }

        var (entities, relationships) = ParseResponse(responseText, unit.Id);
        return (unit.Id, entities, relationships);
    }

    private static (List<Entity> Entities, List<Relationship> Relationships) ParseResponse(string text, string unitId)
    {
        var entities = new List<Entity>();
        var relationships = new List<Relationship>();

        foreach (var record in text.Split(ExtractionPrompts.RecordDelimiter, StringSplitOptions.RemoveEmptyEntries))
        {
            var cleaned = record.Trim().TrimStart('(').TrimEnd(')');
            if (cleaned.StartsWith(ExtractionPrompts.CompletionDelimiter, StringComparison.OrdinalIgnoreCase)) continue;

            var parts = cleaned.Split(ExtractionPrompts.TupleDelimiter);
            if (parts.Length < 2) continue;

            var recordType = parts[0].Trim('"', ' ').ToLowerInvariant();

            if (recordType == "entity" && parts.Length >= 4)
            {
                entities.Add(new Entity
                {
                    Id = Guid.NewGuid().ToString("N"),
                    Name = parts[1].Trim().ToUpperInvariant(),
                    Type = parts[2].Trim().ToUpperInvariant(),
                    Description = parts[3].Trim(),
                    Descriptions = [parts[3].Trim()],
                    TextUnitIds = [unitId],
                });
            }
            else if (recordType == "relationship" && parts.Length >= 5)
            {
                float.TryParse(parts[4].Trim(), out var weight);
                relationships.Add(new Relationship
                {
                    Id = Guid.NewGuid().ToString("N"),
                    Source = parts[1].Trim().ToUpperInvariant(),
                    Target = parts[2].Trim().ToUpperInvariant(),
                    Description = parts[3].Trim(),
                    Descriptions = [parts[3].Trim()],
                    Weight = weight > 0 ? weight : 1.0f,
                    TextUnitIds = [unitId],
                });
            }
        }

        return (entities, relationships);
    }
}
