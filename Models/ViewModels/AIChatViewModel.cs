using TravelAI.Models;

namespace TravelAI.ViewModels;

public sealed class AIChatViewModel
{
    public AIConversation? CurrentConversation { get; set; }
    public List<AIConversation> Conversations { get; set; } = [];
    public bool IsAdmin { get; set; }
    public string ModelName { get; set; } = "";
    public string ControllerName => IsAdmin ? "AdminAI" : "AI";
}
