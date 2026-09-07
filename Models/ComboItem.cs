using System.ComponentModel.DataAnnotations;

namespace TravelAI.Models
{
    public class ComboItem
    {
        public int Id { get; set; }

        public int ComboId { get; set; }

        public Combo? Combo { get; set; }

        [Required]
        [StringLength(100)]
        public string ItemType { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Price { get; set; }

        public string? ImageUrl { get; set; }

        public int SortOrder { get; set; }
    }
}