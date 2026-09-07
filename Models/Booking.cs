using System.ComponentModel.DataAnnotations;

namespace TravelAI.Models
{
    public class Booking
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string BookingCode { get; set; } = string.Empty;

        public int UserId { get; set; }

        public User? User { get; set; }

        public int TourId { get; set; }

        public Tour? Tour { get; set; }

        [Range(1, 10000)]
        public int NumberOfPeople { get; set; }

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