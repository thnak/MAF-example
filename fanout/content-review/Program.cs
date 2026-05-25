using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;

var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
    ?? throw new InvalidOperationException("OPENAI_API_KEY not set");

IChatClient chatClient = new OpenAIClient(new ApiKeyCredential(apiKey))
    .GetChatClient("gpt-4o-mini")
    .AsIChatClient();

var qualityReviewer = new ChatClientAgent(
    chatClient,
    instructions: "You are a quality reviewer. Evaluate the content for clarity, coherence, and technical accuracy. List issues and suggestions.",
    name: "QualityReviewer",
    description: "Reviews content quality and clarity");

var complianceReviewer = new ChatClientAgent(
    chatClient,
    instructions: "You are a compliance reviewer. Check the content for security risks, privacy concerns, and regulatory compliance. Flag any issues.",
    name: "ComplianceReviewer",
    description: "Reviews content for compliance and security");

var styleReviewer = new ChatClientAgent(
    chatClient,
    instructions: "You are a style reviewer. Evaluate the content's tone, voice consistency, and writing style. Suggest improvements.",
    name: "StyleReviewer",
    description: "Reviews content style and tone");

var workflow = AgentWorkflowBuilder.BuildConcurrent(
    "ContentReview",
    [qualityReviewer, complianceReviewer, styleReviewer]);

var content = @"Our new AI solution provides state-of-the-art machine learning capabilities.
It processes data at scale and integrates seamlessly with existing systems.
Users can deploy within hours and see immediate results.";

var input = new List<ChatMessage> { new(ChatRole.User, content) };

Console.WriteLine("=== Fan-Out Multi-Agent Workflow (Parallel Review) ===");
Console.WriteLine($"Content to Review:\n{content}\n");

await using StreamingRun run = await InProcessExecution.RunStreamingAsync(workflow, input);
await run.TrySendMessageAsync(new TurnToken(emitEvents: true));

await foreach (WorkflowEvent evt in run.WatchStreamAsync())
{
    switch (evt)
    {
        case WorkflowOutputEvent output:
            Console.WriteLine("\n=== Review Results ===");
            foreach (var msg in output.As<List<ChatMessage>>() ?? [])
                Console.WriteLine($"[{msg.AuthorName ?? "Agent"}] {msg.Text}");
            break;

        case WorkflowErrorEvent error:
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine($"Workflow error: {error.Exception?.Message}");
            Console.ResetColor();
            break;

        case ExecutorFailedEvent failed:
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine($"Executor '{failed.ExecutorId}' failed: {failed.Data}");
            Console.ResetColor();
            break;
    }
}
