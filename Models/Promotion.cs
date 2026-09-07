using System.ComponentModel.DataAnnotations;

namespace TravelAI.Models
{
    public class Promotion
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Range(0, double.MaxValue)]
        public decimal DiscountValue { get; set; }

        [StringLength(20)]
        public string DiscountType { get; set; } = "Percent";

        [Range(0, double.MaxValue)]
        public decimal? MinimumOrderValue { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        [Range(0, 1000000)]
        public int UsageLimit { get; set; }

        public int UsedCount { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}