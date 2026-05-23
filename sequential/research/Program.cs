using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
    ?? throw new InvalidOperationException("OPENAI_API_KEY not set");

services.AddOpenAIChatClient(modelId: "gpt-4o-mini", apiKey: apiKey);

var sp = services.BuildServiceProvider();
var chatClient = sp.GetRequiredService<IChatClient>();

// Create two agents: one for research, one for summarization
var researchAgent = new HarnessAgent(
    chatClient,
    maxContextWindowTokens: 128_000,
    maxOutputTokens: 8_000,
    new HarnessAgentOptions
    {
        Name = "ResearchAgent",
        Description = "Researches a topic and gathers detailed information",
        ChatOptions = new ChatOptions
        {
            Instructions = "You are a research agent. Use web search to find comprehensive information about the given topic. Present findings in structured sections.",
        }
    }
);

var summaryAgent = new HarnessAgent(
    chatClient,
    maxContextWindowTokens: 128_000,
    maxOutputTokens: 4_000,
    new HarnessAgentOptions
    {
        Name = "SummaryAgent",
        Description = "Summarizes research findings into concise key points",
        ChatOptions = new ChatOptions
        {
            Instructions = "You are a summarization agent. Take detailed research findings and distill them into 5-7 key bullet points. Be concise and actionable.",
        }
    }
);

// Create a workflow that chains the agents: ResearchAgent -> SummaryAgent
var workflowBuilder = new WorkflowBuilder();
var research = workflowBuilder.Track(researchAgent);
var summary = workflowBuilder.Track(summaryAgent);

workflowBuilder.AddChain(research, [summary]);

var workflow = workflowBuilder.Build();

// Run the workflow
var session = new WorkflowSession();
var input = "What are the latest trends in artificial intelligence for 2025?";

Console.WriteLine("=== Sequential Multi-Agent Workflow ===");
Console.WriteLine($"Input: {input}\n");

try
{
    var result = await workflow.ExecuteAsync(session, input);
    Console.WriteLine("=== Workflow Result ===");
    Console.WriteLine(result);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
}
