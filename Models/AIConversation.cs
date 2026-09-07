using System.ComponentModel.DataAnnotations;

namespace TravelAI.Models
{
    public class AIConversation
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public User? User { get; set; }

        [StringLength(200)]
        public string? Title { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        public ICollection<AIChatMessage> Messages { get; set; }
            = new List<AIChatMessage>();
    }
}