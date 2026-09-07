using System.ComponentModel.DataAnnotations;

namespace TravelAI.Models
{
    public class HotelBooking
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string BookingCode { get; set; } = string.Empty;

        public int UserId { get; set; }

        public User? User { get; set; }

        public int HotelId { get; set; }

        public Hotel? Hotel { get; set; }

        public int RoomId { get; set; }

        public Room? Room { get; set; }

        public DateTime CheckInDate { get; set; }

        public DateTime CheckOutDate { get; set; }

        [Range(1, 10000)]
        public int NumberOfGuests { get; set; }

        [Range(1, 10000)]
        public int NumberOfRooms { get; set; }

        [Range(0, double.MaxValue)]
        public decimal TotalPrice { get; set; }

        [Required]
        [StringLength(30)]
        public string Status { get; set; } = "Pending";

        [StringLength(30)]
        public string PaymentStatus { get; set; } = "Unpaid";

        [StringLength(30)]
        public string? PaymentMethod { get; set; }

        public DateTime BookingDate { get; set; } = DateTime.Now;

        public DateTime? CancelledAt { get; set; }

        [StringLength(500)]
        public string? Note { get; set; }
    }
}