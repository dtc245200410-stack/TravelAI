using TravelAI.Models;

namespace TravelAI.Services;

public sealed class AdminAIAgentService(AdminAITools tools)
{
    // Retrieve once, then perform at most one bounded Ollama generation.
    public Task<AIPreparedAnswer> PrepareAsync(string question,
        IReadOnlyList<AIHistoryMessage> history, CancellationToken ct) => tools.GetReportAsync(question, history, ct);
}
