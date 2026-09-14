using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelAI.Data;

namespace TravelAI.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminStatisticsController : Controller
    {
        private readonly TravelDbContext _context;

        public AdminStatisticsController(TravelDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // ==============================
            // THỐNG KÊ TỔNG QUAN
            // ==============================

            var totalUsers = await _context.Users.CountAsync();

            var totalTours = await _context.Tours.CountAsync();

            var totalHotels = await _context.Hotels.CountAsync();

            var totalRooms = await _context.Rooms.CountAsync();

            var totalTourBookings = await _context.Bookings.CountAsync();

            var totalHotelBookings = await _context.HotelBookings.CountAsync();


            // ==============================
            // THỐNG KÊ BOOKING TOUR
            // ==============================

            var pendingTourBookings = await _context.Bookings
                .CountAsync(b => b.Status == "Pending");

            var confirmedTourBookings = await _context.Bookings
                .CountAsync(b => b.Status == "Confirmed");

            var cancelledTourBookings = await _context.Bookings
                .CountAsync(b => b.Status == "Cancelled");


            // ==============================
            // DOANH THU TOUR
            // ==============================

            var tourRevenue = await _context.Bookings
                .Where(b =>
                    b.Status != "Cancelled" &&
                    b.PaymentStatus == "Paid")
                .SumAsync(b => (decimal?)b.TotalPrice) ?? 0;


            // ==============================
            // DOANH THU KHÁCH SẠN
            // ==============================

            var hotelRevenue = await _context.HotelBookings
                .Where(b =>
                    b.Status != "Cancelled" &&
                    b.PaymentStatus == "Paid")
                .SumAsync(b => (decimal?)b.TotalPrice) ?? 0;


            // ==============================
            // TỔNG DOANH THU
            // ==============================

            var totalRevenue = tourRevenue + hotelRevenue;


            // ==============================
            // TRUYỀN DỮ LIỆU SANG VIEW
            // ==============================

            ViewBag.TotalUsers = totalUsers;
            ViewBag.TotalTours = totalTours;
            ViewBag.TotalHotels = totalHotels;
            ViewBag.TotalRooms = totalRooms;

            ViewBag.TotalTourBookings = totalTourBookings;
            ViewBag.TotalHotelBookings = totalHotelBookings;

            ViewBag.PendingTourBookings = pendingTourBookings;
            ViewBag.ConfirmedTourBookings = confirmedTourBookings;
            ViewBag.CancelledTourBookings = cancelledTourBookings;

            ViewBag.TourRevenue = tourRevenue;
            ViewBag.HotelRevenue = hotelRevenue;
            ViewBag.TotalRevenue = totalRevenue;


            return View();
        }
    }
}