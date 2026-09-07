using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelAI.Data;

namespace TravelAI.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminBookingController : Controller
    {
        private readonly TravelDbContext _context;

        public AdminBookingController(TravelDbContext context)
        {
            _context = context;
        }

        // =========================
        // DANH SÁCH BOOKING
        // =========================

        public async Task<IActionResult> Index(
            string? searchString,
            string? status,
            string? paymentStatus)
        {
            var bookings = _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Tour)
                .AsQueryable();

            // TÌM KIẾM
            if (!string.IsNullOrWhiteSpace(searchString))
            {
                searchString = searchString.Trim();

                bookings = bookings.Where(b =>
                    b.BookingCode.Contains(searchString) ||
                    (b.User != null &&
                     (
                         b.User.FullName.Contains(searchString) ||
                         b.User.Email.Contains(searchString)
                     )) ||
                    (b.Tour != null &&
                     b.Tour.Name.Contains(searchString)));
            }

            // LỌC TRẠNG THÁI BOOKING
            if (!string.IsNullOrWhiteSpace(status) &&
                status != "All")
            {
                bookings = bookings.Where(b =>
                    b.Status == status);
            }

            // LỌC TRẠNG THÁI THANH TOÁN
            if (!string.IsNullOrWhiteSpace(paymentStatus) &&
                paymentStatus != "All")
            {
                bookings = bookings.Where(b =>
                    b.PaymentStatus == paymentStatus);
            }

            var result = await bookings
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();

            ViewBag.SearchString = searchString;
            ViewBag.Status = status ?? "All";
            ViewBag.PaymentStatus = paymentStatus ?? "All";

            return View(result);
        }

        // =========================
        // CHI TIẾT BOOKING
        // =========================

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var booking = await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Tour)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null)
            {
                return NotFound();
            }

            return View(booking);
        }

        // =========================
        // XÁC NHẬN BOOKING
        // =========================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirm(int id)
        {
            var booking = await _context.Bookings
                .Include(b => b.Tour)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null)
            {
                return NotFound();
            }

            if (booking.Status == "Cancelled")
            {
                TempData["Error"] =
                    "Booking đã bị hủy, không thể xác nhận.";

                return RedirectToAction(nameof(Index));
            }

            if (booking.Status == "Confirmed")
            {
                TempData["Error"] =
                    "Booking này đã được xác nhận.";

                return RedirectToAction(nameof(Index));
            }

            booking.Status = "Confirmed";

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Xác nhận Booking thành công.";

            return RedirectToAction(nameof(Index));
        }

        // =========================
        // HỦY BOOKING
        // =========================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var booking = await _context.Bookings
                .Include(b => b.Tour)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null)
            {
                return NotFound();
            }

            if (booking.Status == "Cancelled")
            {
                TempData["Error"] =
                    "Booking này đã được hủy trước đó.";

                return RedirectToAction(nameof(Index));
            }

            booking.Status = "Cancelled";
            booking.CancelledAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Hủy Booking thành công.";

            return RedirectToAction(nameof(Index));
        }

        // =========================
        // ĐÁNH DẤU ĐÃ THANH TOÁN
        // =========================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsPaid(int id)
        {
            var booking = await _context.Bookings
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null)
            {
                return NotFound();
            }

            if (booking.Status == "Cancelled")
            {
                TempData["Error"] =
                    "Booking đã bị hủy, không thể cập nhật thanh toán.";

                return RedirectToAction(nameof(Index));
            }

            booking.PaymentStatus = "Paid";

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Đã cập nhật trạng thái thanh toán.";

            return RedirectToAction(nameof(Index));
        }
    }
}