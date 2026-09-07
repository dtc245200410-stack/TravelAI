using System.ComponentModel.DataAnnotations;

namespace TravelAI.Models
{
    public class TourImage
    {
        public int Id { get; set; }

        [Required]
        public string ImageUrl { get; set; } = string.Empty;

        public int SortOrder { get; set; }

        // Khóa ngoại
        public int TourId { get; set; }

        // Quan hệ với Tour
        public Tour? Tour { get; set; }
    }
}