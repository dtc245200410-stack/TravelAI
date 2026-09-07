using System.ComponentModel.DataAnnotations;

namespace TravelAI.Models
{
    public class Room
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string RoomName { get; set; } = string.Empty;

        [StringLength(50)]
        public string? RoomType { get; set; }

        [Range(1, 100)]
        public int MaxGuests { get; set; }

        [Range(0, double.MaxValue)]
        public decimal PricePerNight { get; set; }

        [Range(0, 10000)]
        public int Quantity { get; set; }

        [Range(0, 10000)]
        public int AvailableQuantity { get; set; }

        public string? Description { get; set; }

        public string? ImageUrl { get; set; }

        [StringLength(30)]
        public string Status { get; set; } = "Available";

        public int HotelId { get; set; }

        public Hotel? Hotel { get; set; }
    }
}