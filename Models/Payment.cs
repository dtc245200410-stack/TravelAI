using System.ComponentModel.DataAnnotations;

namespace TravelAI.Models
{
    public class Payment
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string PaymentCode { get; set; } = string.Empty;

        public int UserId { get; set; }

        public User? User { get; set; }

        public int? BookingId { get; set; }

        public Booking? Booking { get; set; }

        public int? HotelBookingId { get; set; }

        public HotelBooking? HotelBooking { get; set; }

        public int? ComboId { get; set; }

        public Combo? Combo { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Amount { get; set; }

        [Required]
        [StringLength(30)]
        public string PaymentMethod { get; set; } = "Cash";

        [Required]
        [StringLength(30)]
        public string Status { get; set; } = "Pending";

        [StringLength(100)]
        public string? TransactionId { get; set; }

        public DateTime PaymentDate { get; set; } = DateTime.Now;

        [StringLength(500)]
        public string? Note { get; set; }
    }
}