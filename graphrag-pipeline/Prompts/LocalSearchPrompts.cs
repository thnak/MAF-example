namespace GraphRag.Pipeline.Prompts;

public static class LocalSearchPrompts
{
    public static string Build(string context, string query) =>
        $"""
        You are a helpful assistant answering questions about a knowledge graph.

        Use only the context below to answer the question. If the context does not contain enough
        information, say so clearly rather than guessing.

        ---CONTEXT---
        {context}
        ---END CONTEXT---

        Question: {query}

        Answer:
        """;
}
