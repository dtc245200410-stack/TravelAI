using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using TravelAI.Data;
using TravelAI.Models;
using TravelAI.Services;

namespace TravelAI.Controllers;

[Authorize(Roles = "Admin")]
public sealed class AdminAIController(TravelDbContext db, OllamaService ollama,
    TravelAIService travel, AdminAIAgentService admin, AIRequestGate gate,
    IOptions<AIOptions> options, ILoggerFactory logger)
    : AIChatControllerBase(db, ollama, travel, admin, gate, options, logger)
{
    protected override string ChatScope => "Admin";
}
