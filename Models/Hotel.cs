using Microsoft.AspNetCore.Mvc.ViewEngines;
using System.ComponentModel.DataAnnotations;

namespace TravelAI.Models
{
    public class Hotel
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Slug { get; set; }

        public string? Description { get; set; }

        [Required]
        [StringLength(300)]
        public string Address { get; set; } = string.Empty;

        [StringLength(100)]
        public string? City { get; set; }

        [StringLength(100)]
        public string? Province { get; set; }

        public string? ImageUrl { get; set; }

        [Range(1, 5)]
        public double Rating { get; set; }

        public int ReviewCount { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [StringLength(30)]
        public string Status { get; set; } = "Active";

        public int DestinationId { get; set; }

        public Destination? Destination { get; set; }

        public ICollection<Room> Rooms { get; set; }
            = new List<Room>();

        public ICollection<HotelBooking> HotelBookings { get; set; }
            = new List<HotelBooking>();

        public ICollection<Review> Reviews { get; set; }
            = new List<Review>();

        public ICollection<Favorite> Favorites { get; set; }
            = new List<Favorite>();
    }
}