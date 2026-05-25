using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace StructuredOutput.JsonRepair;

/// <summary>
/// Delegating agent that runs LlmJsonRepair on every response before returning it.
/// Wrap any AIAgent with this to transparently fix malformed JSON from the LLM.
/// </summary>
public sealed class JsonRepairMiddleware(AIAgent innerAgent) : DelegatingAIAgent(innerAgent)
{
    protected override async Task<AgentResponse> RunCoreAsync(
        IEnumerable<ChatMessage> messages,
        AgentSession? session = null,
        AgentRunOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var inner = await InnerAgent.RunAsync(messages, session, options, cancellationToken);
        var repaired = LlmJsonRepair.Repair(inner.Text);

        return new AgentResponse(new ChatMessage(ChatRole.Assistant, repaired))
        {
            AgentId = inner.AgentId,
            ResponseId = inner.ResponseId,
            FinishReason = inner.FinishReason,
            Usage = inner.Usage,
            CreatedAt = inner.CreatedAt,
            AdditionalProperties = inner.AdditionalProperties,
        };
    }
}

/// <summary>
/// AIAgentBuilder extension for adding JsonRepairMiddleware to an agent pipeline.
/// </summary>
public static class JsonRepairAgentBuilderExtensions
{
    /// <summary>
    /// Adds <see cref="JsonRepairMiddleware"/> to the agent pipeline.
    /// Call this after the inner agent but before .Build() — e.g.
    /// <c>agent.AsBuilder().UseJsonRepair().Build()</c>
    /// </summary>
    public static AIAgentBuilder UseJsonRepair(this AIAgentBuilder builder)
        => builder.Use(inner => new JsonRepairMiddleware(inner));
}
