using Microsoft.AspNetCore.Mvc.ViewEngines;
using System.ComponentModel.DataAnnotations;

namespace TravelAI.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Phone]
        [StringLength(20)]
        public string? Phone { get; set; }

        public string? Avatar { get; set; }

        [Required]
        [StringLength(20)]
        public string Role { get; set; } = "Customer";

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Quan hệ
        public ICollection<Booking> Bookings { get; set; }
            = new List<Booking>();

        public ICollection<HotelBooking> HotelBookings { get; set; }
            = new List<HotelBooking>();

        public ICollection<Review> Reviews { get; set; }
            = new List<Review>();

        public ICollection<Favorite> Favorites { get; set; }
            = new List<Favorite>();
    }
}