using Microsoft.AspNetCore.Mvc.ViewEngines;
using System.ComponentModel.DataAnnotations;

namespace TravelAI.Models
{
    public class Tour
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Slug { get; set; }

        public string? Description { get; set; }

        // Giá gốc
        [Range(0, double.MaxValue)]
        public decimal Price { get; set; }

        // Giá khuyến mãi
        [Range(0, double.MaxValue)]
        public decimal? SalePrice { get; set; }

        // Số ngày của tour
        [Range(1, 365)]
        public int Duration { get; set; }

        // Nơi khởi hành
        [StringLength(150)]
        public string? DepartureLocation { get; set; }

        // Ngày khởi hành
        public DateTime DepartureDate { get; set; }

        // Số người tối đa
        [Range(1, 10000)]
        public int MaxPeople { get; set; }

        // Số chỗ còn lại
        [Range(0, 10000)]
        public int AvailablePeople { get; set; }

        // Ảnh đại diện
        public string? ImageUrl { get; set; }

        // Điểm đánh giá trung bình
        [Range(0, 5)]
        public double Rating { get; set; }

        // Số lượng đánh giá
        public int ReviewCount { get; set; }

        // Ngày tạo tour
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Trạng thái
        [StringLength(30)]
        public string Status { get; set; } = "Active";

        // Khóa ngoại đến Destination
        public int DestinationId { get; set; }

        // Quan hệ với Destination
        public Destination? Destination { get; set; }

        // Một tour có nhiều hình ảnh
        public ICollection<TourImage> Images { get; set; }
            = new List<TourImage>();

        // Một tour có nhiều đơn đặt
        public ICollection<Booking> Bookings { get; set; }
            = new List<Booking>();

        // Một tour có nhiều đánh giá
        public ICollection<Review> Reviews { get; set; }
            = new List<Review>();

        // Một tour có thể được nhiều người yêu thích
        public ICollection<Favorite> Favorites { get; set; }
            = new List<Favorite>();
    }
}