namespace GraphRag.Pipeline.Prompts;

public static class SummarizationPrompts
{
    public static string Build(string entityName, IEnumerable<string> descriptions) =>
        $"""
        You are a helpful assistant generating a comprehensive summary from multiple descriptions of the same entity.

        Entity: {entityName}
        Descriptions:
        {string.Join("\n- ", descriptions.Select((d, i) => $"{i + 1}. {d}"))}

        Write a single, comprehensive description in third person. Resolve any contradictions and combine all information.
        Output only the summary text, nothing else.
        """;
}
