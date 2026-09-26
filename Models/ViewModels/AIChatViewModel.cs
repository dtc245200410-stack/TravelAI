using TravelAI.Models;

namespace TravelAI.ViewModels
{
    public class AIChatViewModel
    {
        // Cuộc trò chuyện đang được mở
        public AIConversation? CurrentConversation { get; set; }

        // Danh sách các cuộc trò chuyện của người dùng
        public List<AIConversation> Conversations { get; set; }
            = new List<AIConversation>();
    }
}