using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using TravelAI.Data;
using TravelAI.Models;
using TravelAI.Services;

namespace TravelAI.Controllers;

[Authorize]
public sealed class AIController(TravelDbContext db, OllamaService ollama,
    TravelAIService travel, AdminAIAgentService admin, AIRequestGate gate,
    IOptions<AIOptions> options, ILoggerFactory logger)
    : AIChatControllerBase(db, ollama, travel, admin, gate, options, logger)
{
    protected override string ChatScope => "Travel";
}
