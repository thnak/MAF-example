namespace GraphRag.Pipeline.Prompts;

public static class CommunityReportPrompts
{
    public static string Build(string contextData) =>
        $$"""
        You are an AI assistant that analyzes communities of entities and generates structured reports.

        -Goal-
        Write a comprehensive assessment of a community of entities based on their descriptions and relationships.

        -Report Structure-
        Return valid JSON in this exact format (no markdown, no code blocks):
        {
          "title": "<short but specific community name>",
          "summary": "<executive summary of the community's overall structure and significance>",
          "findings": [
            { "summary": "<finding title>", "explanation": "<detailed explanation>" }
          ]
        }

        Include 3 to 7 findings. Output only the JSON object.

        -Community Data-
        {{contextData}}
        """;
}
