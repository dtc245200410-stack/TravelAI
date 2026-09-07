using System.ComponentModel.DataAnnotations;

namespace TravelAI.Models
{
    public class AIChatMessage
    {
        public int Id { get; set; }

        public int ConversationId { get; set; }

        public AIConversation? Conversation { get; set; }

        [Required]
        [StringLength(20)]
        public string Role { get; set; } = "User";

        [Required]
        public string Content { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}