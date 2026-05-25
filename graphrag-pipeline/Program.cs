using GraphRag.Pipeline.Abstractions;
using GraphRag.Pipeline.Executors;
using GraphRag.Pipeline.InMemory;
using GraphRag.Pipeline.Models;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;

var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
    ?? throw new InvalidOperationException("OPENAI_API_KEY not set");

IChatClient chatClient = new OpenAIClient(new ApiKeyCredential(apiKey))
    .GetChatClient("gpt-4o-mini")
    .AsIChatClient();

// Storage — swap with real implementations (MongoDB, PostgreSQL, LanceDB, etc.)
var graphStore = new InMemoryGraphStore();
IVectorStore vectorStore = new InMemoryVectorStore();
IEmbeddingModel embeddingModel = new StubEmbeddingModel();     // swap: OpenAI text-embedding-3-small
ICommunityDetector communityDetector = new GreedyCommunityDetector(); // swap: Leiden/Louvain
IGraphReader graphReader = new InMemoryGraphReader(graphStore);

var config = new PipelineConfig
{
    ChunkSize = 150,       // words per chunk
    ChunkOverlap = 20,     // word overlap between chunks
    MaxGleanings = 1,      // extra LLM passes to find missed entities
    MaxParallelExtraction = 2,
    EntityTypes = ["organization", "person", "location", "concept"],
};

// Build executors
var chunking = new ChunkingExecutor(config);
var extraction = new ExtractionExecutor(chatClient, config);
var merge = new MergeExecutor();
var summarization = new SummarizationExecutor(chatClient);
var cluster = new ClusterExecutor(communityDetector);
var communityReport = new CommunityReportExecutor(chatClient);
var embed = new EmbedExecutor(embeddingModel, vectorStore, graphStore);
var search = new SearchExecutor(embeddingModel, vectorStore, graphReader, chatClient);

// Wire the linear pipeline using WorkflowBuilder
var workflow = new WorkflowBuilder(chunking)
    .AddEdge(chunking, extraction)
    .AddEdge(extraction, merge)
    .AddEdge(merge, summarization)
    .AddEdge(summarization, cluster)
    .AddEdge(cluster, communityReport)
    .AddEdge(communityReport, embed)
    .WithOutputFrom(embed)
    .Build();

var input = new PipelineInput
{
    Documents =
    [
        new Document
        {
            Id = "doc1",
            Text = "Microsoft Corporation is a technology company founded by Bill Gates and Paul Allen in 1975. " +
                   "Headquartered in Redmond, Washington, Microsoft develops software, services, and hardware. " +
                   "Satya Nadella became CEO in 2014, leading a major cloud transformation strategy with Azure.",
        },
        new Document
        {
            Id = "doc2",
            Text = "OpenAI was founded in 2015 by Sam Altman, Elon Musk, Ilya Sutskever, and Greg Brockman. " +
                   "The company created GPT-4 and ChatGPT. Microsoft made a major investment in OpenAI " +
                   "and integrated its models into Azure OpenAI Service and Microsoft 365 Copilot.",
        },
    ]
};

Console.WriteLine("=== GraphRAG Pipeline (Microsoft Agent Framework) ===");
Console.WriteLine($"Documents: {input.Documents.Length}\n");

await using var run = await InProcessExecution.RunAsync(workflow, input);

foreach (var evt in run.NewEvents)
{
    switch (evt)
    {
        case WorkflowOutputEvent { Data: IndexingResult result }:
            Console.WriteLine("\n=== Indexing Complete ===");
            Console.WriteLine($"Entities:      {result.EntityCount}");
            Console.WriteLine($"Relationships: {result.RelationshipCount}");
            Console.WriteLine($"Communities:   {result.CommunityCount}");
            Console.WriteLine($"Vectors:       {result.VectorCount}");
            Console.WriteLine($"Elapsed:       {result.Elapsed.TotalSeconds:F1}s");
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

// === Local Search Demo ===
Console.WriteLine("\n=== Local Search Demo ===");

var searchWorkflow = new WorkflowBuilder(search)
    .WithOutputFrom(search)
    .Build();

string[] queries =
[
    "Who founded Microsoft and what is the company known for?",
    "What is the relationship between OpenAI and Microsoft?",
];

foreach (var query in queries)
{
    Console.WriteLine($"\nQ: {query}");
    await using var searchRun = await InProcessExecution.RunAsync(searchWorkflow,
        new SearchInput { Query = query });

    foreach (var evt in searchRun.NewEvents)
    {
        if (evt is WorkflowOutputEvent { Data: SearchResult result })
            Console.WriteLine($"A: {result.Answer}");
        else if (evt is WorkflowErrorEvent err)
            Console.Error.WriteLine($"Search error: {err.Exception?.Message}");
        else if (evt is ExecutorFailedEvent fail)
            Console.Error.WriteLine($"Search executor failed: {fail.Data}");
    }
}
