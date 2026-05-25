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

var researchAgent = new ChatClientAgent(
    chatClient,
    instructions: "You are a research agent. Find comprehensive information about the given topic. Present findings in structured sections.",
    name: "ResearchAgent",
    description: "Researches a topic and gathers detailed information");

var summaryAgent = new ChatClientAgent(
    chatClient,
    instructions: "You are a summarization agent. Take detailed research findings and distill them into 5-7 key bullet points. Be concise and actionable.",
    name: "SummaryAgent",
    description: "Summarizes research findings into concise key points");

var workflow = AgentWorkflowBuilder.BuildSequential("ResearchAndSummarize", researchAgent, summaryAgent);

var input = new List<ChatMessage> { new(ChatRole.User, "What are the latest trends in artificial intelligence for 2025?") };

Console.WriteLine("=== Sequential Multi-Agent Workflow ===");
Console.WriteLine($"Input: {input[0].Text}\n");

await using StreamingRun run = await InProcessExecution.RunStreamingAsync(workflow, input);
await run.TrySendMessageAsync(new TurnToken(emitEvents: true));

await foreach (WorkflowEvent evt in run.WatchStreamAsync())
{
    switch (evt)
    {
        case WorkflowOutputEvent output:
            Console.WriteLine("\n=== Workflow Result ===");
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
