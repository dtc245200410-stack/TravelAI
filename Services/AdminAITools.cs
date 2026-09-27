using Microsoft.EntityFrameworkCore;
using TravelAI.Data;

namespace TravelAI.Services
{
    public class AdminAITools
    {
        private readonly TravelDbContext _context;

        public AdminAITools(TravelDbContext context)
        {
            _context = context;
        }

        // 1. Lấy tổng số người dùng
        public async Task<int> GetUserCountAsync()
        {
            return await _context.Users.CountAsync();
        }

        // 2. Lấy tổng số tour
        public async Task<int> GetTourCountAsync()
        {
            return await _context.Tours.CountAsync();
        }

        // 3. Lấy tổng số khách sạn
        public async Task<int> GetHotelCountAsync()
        {
            return await _context.Hotels.CountAsync();
        }

        // 4. Lấy tổng số phòng
        public async Task<int> GetRoomCountAsync()
        {
            return await _context.Rooms.CountAsync();
        }

        // 5. Lấy thống kê booking tour
        public async Task<object> GetBookingStatisticsAsync()
        {
            var total = await _context.Bookings.CountAsync();

            var pending = await _context.Bookings
                .CountAsync(x => x.Status == "Pending");

            var confirmed = await _context.Bookings
                .CountAsync(x => x.Status == "Confirmed");

            var cancelled = await _context.Bookings
                .CountAsync(x => x.Status == "Cancelled");

            return new
            {
                Total = total,
                Pending = pending,
                Confirmed = confirmed,
                Cancelled = cancelled
            };
        }

        // 6. Lấy tổng doanh thu từ booking đã xác nhận
        public async Task<decimal> GetRevenueAsync()
        {
            return await _context.Bookings
                .Where(x => x.Status == "Confirmed")
                .SumAsync(x => (decimal?)x.TotalPrice) ?? 0;
        }
    }
}