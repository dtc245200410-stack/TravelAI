using System.ComponentModel.DataAnnotations;

namespace TravelAI.Models
{
    public class Destination
    {
        public int Id { get; set; }

        [Required]
        [StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [StringLength(150)]
        public string? Slug { get; set; }

        public string? Description { get; set; }

        public string? ImageUrl { get; set; }

        [StringLength(100)]
        public string? Province { get; set; }

        [StringLength(50)]
        public string? Region { get; set; }

        // Quan hệ
        public ICollection<Tour> Tours { get; set; }
            = new List<Tour>();

        public ICollection<Hotel> Hotels { get; set; }
            = new List<Hotel>();
    }
}