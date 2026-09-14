using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelAI.Data;

namespace TravelAI.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminPaymentController : Controller
    {
        private readonly TravelDbContext _context;

        public AdminPaymentController(TravelDbContext context)
        {
            _context = context;
        }

        // Danh sách thanh toán
        public async Task<IActionResult> Index()
        {
            var payments = await _context.Payments
                .Include(p => p.User)
                .Include(p => p.Booking)
                .Include(p => p.HotelBooking)
                .Include(p => p.Combo)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

            return View(payments);
        }

        // Xác nhận thanh toán
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirm(int id)
        {
            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.Id == id);

            if (payment == null)
            {
                return NotFound();
            }

            payment.Status = "Paid";

            // Nếu là Booking Tour
            if (payment.BookingId.HasValue)
            {
                var booking = await _context.Bookings
                    .FirstOrDefaultAsync(b => b.Id == payment.BookingId.Value);

                if (booking != null)
                {
                    booking.PaymentStatus = "Paid";

                    if (booking.Status == "Pending")
                    {
                        booking.Status = "Confirmed";
                    }
                }
            }

            // Nếu là Booking khách sạn
            if (payment.HotelBookingId.HasValue)
            {
                var hotelBooking = await _context.HotelBookings
                    .FirstOrDefaultAsync(h => h.Id == payment.HotelBookingId.Value);

                if (hotelBooking != null)
                {
                    hotelBooking.PaymentStatus = "Paid";

                    if (hotelBooking.Status == "Pending")
                    {
                        hotelBooking.Status = "Confirmed";
                    }
                }
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã xác nhận thanh toán thành công.";

            return RedirectToAction(nameof(Index));
        }

        // Hủy thanh toán
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.Id == id);

            if (payment == null)
            {
                return NotFound();
            }

            payment.Status = "Cancelled";

            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã hủy giao dịch thanh toán.";

            return RedirectToAction(nameof(Index));
        }
    }
}