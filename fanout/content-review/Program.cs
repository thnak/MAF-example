using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
    ?? throw new InvalidOperationException("OPENAI_API_KEY not set");

services.AddOpenAIChatClient(modelId: "gpt-4o-mini", apiKey: apiKey);

var sp = services.BuildServiceProvider();
var chatClient = sp.GetRequiredService<IChatClient>();

// Create three review agents with different perspectives
var qualityReviewer = new HarnessAgent(
    chatClient,
    maxContextWindowTokens: 128_000,
    maxOutputTokens: 4_000,
    new HarnessAgentOptions
    {
        Name = "QualityReviewer",
        Description = "Reviews content quality and clarity",
        ChatOptions = new ChatOptions
        {
            Instructions = "You are a quality reviewer. Evaluate the content for clarity, coherence, and technical accuracy. List issues and suggestions.",
        }
    }
);

var complianceReviewer = new HarnessAgent(
    chatClient,
    maxContextWindowTokens: 128_000,
    maxOutputTokens: 4_000,
    new HarnessAgentOptions
    {
        Name = "ComplianceReviewer",
        Description = "Reviews content for compliance and security",
        ChatOptions = new ChatOptions
        {
            Instructions = "You are a compliance reviewer. Check the content for security risks, privacy concerns, and regulatory compliance. Flag any issues.",
        }
    }
);

var styleReviewer = new HarnessAgent(
    chatClient,
    maxContextWindowTokens: 128_000,
    maxOutputTokens: 4_000,
    new HarnessAgentOptions
    {
        Name = "StyleReviewer",
        Description = "Reviews content style and tone",
        ChatOptions = new ChatOptions
        {
            Instructions = "You are a style reviewer. Evaluate the content's tone, voice consistency, and writing style. Suggest improvements.",
        }
    }
);

// Create a workflow with fan-out pattern: one coordinator sends to three reviewers in parallel
var workflowBuilder = new WorkflowBuilder();
var quality = workflowBuilder.Track(qualityReviewer);
var compliance = workflowBuilder.Track(complianceReviewer);
var style = workflowBuilder.Track(styleReviewer);

// Fan-out: one source connects to multiple reviewers
workflowBuilder.ForwardMessage<string>(quality, [compliance, style]);

var workflow = workflowBuilder.Build();

// Run the workflow
var session = new WorkflowSession();
var content = @"
Our new AI solution provides state-of-the-art machine learning capabilities.
It processes data at scale and integrates seamlessly with existing systems.
Users can deploy within hours and see immediate results.
";

Console.WriteLine("=== Fan-Out Multi-Agent Workflow (Parallel Review) ===");
Console.WriteLine($"Content to Review:\n{content}\n");

try
{
    var result = await workflow.ExecuteAsync(session, content);
    Console.WriteLine("=== Review Results ===");
    Console.WriteLine(result);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
}
