using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelAI.Data;

namespace TravelAI.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly TravelDbContext _context;

        public AdminController(TravelDbContext context)
        {
            _context = context;
        }

        // ================= DASHBOARD =================

        public async Task<IActionResult> Index()
        {
            var totalUsers = await _context.Users.CountAsync();

            var totalTours = await _context.Tours.CountAsync();

            var totalHotels = await _context.Hotels.CountAsync();

            var totalRooms = await _context.Rooms.CountAsync();

            var totalTourBookings = await _context.Bookings.CountAsync();

            var totalHotelBookings = await _context.HotelBookings.CountAsync();

            var pendingTourBookings = await _context.Bookings
                .CountAsync(x => x.Status == "Pending");

            var confirmedTourBookings = await _context.Bookings
                .CountAsync(x => x.Status == "Confirmed");

            var cancelledTourBookings = await _context.Bookings
                .CountAsync(x => x.Status == "Cancelled");

            var tourRevenue = await _context.Bookings
                .Where(x =>
                    x.Status != "Cancelled" &&
                    x.PaymentStatus == "Paid")
                .SumAsync(x => (decimal?)x.TotalPrice) ?? 0;

            var hotelRevenue = await _context.HotelBookings
                .Where(x =>
                    x.Status != "Cancelled" &&
                    x.PaymentStatus == "Paid")
                .SumAsync(x => (decimal?)x.TotalPrice) ?? 0;

            var totalRevenue = tourRevenue + hotelRevenue;

            ViewBag.TotalUsers = totalUsers;
            ViewBag.TotalTours = totalTours;
            ViewBag.TotalHotels = totalHotels;
            ViewBag.TotalRooms = totalRooms;

            ViewBag.TotalTourBookings = totalTourBookings;
            ViewBag.TotalHotelBookings = totalHotelBookings;

            ViewBag.PendingBookings = pendingTourBookings;
            ViewBag.ConfirmedBookings = confirmedTourBookings;
            ViewBag.CancelledBookings = cancelledTourBookings;

            ViewBag.TourRevenue = tourRevenue;
            ViewBag.HotelRevenue = hotelRevenue;
            ViewBag.TotalRevenue = totalRevenue;

            return View();
        }
    }
}