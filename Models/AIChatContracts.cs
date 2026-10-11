using System.ComponentModel.DataAnnotations;

namespace TravelAI.Models;

public sealed class AIChatRequest
{
    public int? ConversationId { get; set; }
    [Required, StringLength(2000)]
    public string Message { get; set; } = "";
    public bool Regenerate { get; set; }
}

public sealed record AIHistoryMessage(string Role, string Content);
public sealed record AIActionLink(string Label, string Url);
public sealed record AITourCard(int Id, string Name, string Destination, decimal Price,
    int Duration, DateTime DepartureDate, int AvailablePeople, string Url);
public sealed class AIPreparedAnswer
{
    public string SystemPrompt { get; set; } = "";
    public string Evidence { get; set; } = "";
    public string GroundedAnswer { get; set; } = "";
    public List<AITourCard> Tours { get; set; } = [];
    public List<AIActionLink> Actions { get; set; } = [];
}
