using System.ComponentModel.DataAnnotations;

namespace TravelAI.Models
{
    public class Review
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public User? User { get; set; }

        public int? TourId { get; set; }

        public Tour? Tour { get; set; }

        public int? HotelId { get; set; }

        public Hotel? Hotel { get; set; }

        [Range(1, 5)]
        public int Rating { get; set; }

        [StringLength(1000)]
        public string? Comment { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [StringLength(30)]
        public string Status { get; set; } = "Approved";
    }
}