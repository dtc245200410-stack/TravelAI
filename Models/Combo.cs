using System.ComponentModel.DataAnnotations;

namespace TravelAI.Models
{
    public class Combo
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Slug { get; set; }

        public string? Description { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Price { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? SalePrice { get; set; }

        [Range(1, 365)]
        public int Duration { get; set; }

        public string? ImageUrl { get; set; }

        [StringLength(150)]
        public string? DepartureLocation { get; set; }

        [StringLength(150)]
        public string? Destination { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        [Range(1, 10000)]
        public int MaxPeople { get; set; }

        [Range(0, 10000)]
        public int AvailablePeople { get; set; }

        [Range(0, 5)]
        public double Rating { get; set; }

        public int ReviewCount { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [StringLength(30)]
        public string Status { get; set; } = "Active";

        public ICollection<ComboItem> Items { get; set; }
            = new List<ComboItem>();

        public ICollection<Payment> Payments { get; set; }
            = new List<Payment>();
    }
}