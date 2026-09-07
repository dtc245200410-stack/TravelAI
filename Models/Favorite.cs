using System.ComponentModel.DataAnnotations;

namespace TravelAI.Models
{
    public class Favorite
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public User? User { get; set; }

        public int? TourId { get; set; }

        public Tour? Tour { get; set; }

        public int? HotelId { get; set; }

        public Hotel? Hotel { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}