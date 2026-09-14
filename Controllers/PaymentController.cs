using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TravelAI.Data;

namespace TravelAI.Controllers
{
    [Authorize]
    public class PaymentController : Controller
    {
        private readonly TravelDbContext _context;

        public PaymentController(TravelDbContext context)
        {
            _context = context;
        }

        // Hiển thị trang thanh toán
        [HttpGet]
        public async Task<IActionResult> Create(int? bookingId)
        {
            if (bookingId == null)
            {
                return NotFound();
            }

            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdString, out var userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var booking = await _context.Bookings
                .Include(b => b.Tour)
                .FirstOrDefaultAsync(b =>
                    b.Id == bookingId &&
                    b.UserId == userId);

            if (booking == null)
            {
                return NotFound();
            }

            // Không cho thanh toán đơn đã hủy
            if (booking.Status == "Cancelled")
            {
                TempData["Error"] = "Đơn đặt tour này đã bị hủy.";
                return RedirectToAction(
                    "Details",
                    "Booking",
                    new { id = booking.Id });
            }

            // Không cho thanh toán lại đơn đã thanh toán
            if (booking.PaymentStatus == "Paid")
            {
                TempData["Error"] = "Đơn đặt tour này đã được thanh toán.";
                return RedirectToAction(
                    "Details",
                    "Booking",
                    new { id = booking.Id });
            }

            return View(booking);
        }

        // Tạo giao dịch thanh toán
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            int bookingId,
            string paymentMethod,
            string? note)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdString, out var userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var booking = await _context.Bookings
                .FirstOrDefaultAsync(b =>
                    b.Id == bookingId &&
                    b.UserId == userId);

            if (booking == null)
            {
                return NotFound();
            }

            if (booking.Status == "Cancelled")
            {
                TempData["Error"] = "Đơn đặt tour này đã bị hủy.";

                return RedirectToAction(
                    "Details",
                    "Booking",
                    new { id = booking.Id });
            }

            if (booking.PaymentStatus == "Paid")
            {
                TempData["Error"] = "Đơn đặt tour này đã được thanh toán.";

                return RedirectToAction(
                    "Details",
                    "Booking",
                    new { id = booking.Id });
            }

            // Kiểm tra giao dịch đang chờ xử lý
            var existingPayment = await _context.Payments
                .FirstOrDefaultAsync(p =>
                    p.BookingId == booking.Id &&
                    p.Status == "Pending");

            if (existingPayment != null)
            {
                TempData["Error"] =
                    "Đơn này đã có một giao dịch thanh toán đang chờ xử lý.";

                return RedirectToAction(
                    "Details",
                    "Booking",
                    new { id = booking.Id });
            }

            if (string.IsNullOrWhiteSpace(paymentMethod))
            {
                TempData["Error"] = "Vui lòng chọn phương thức thanh toán.";

                return RedirectToAction(
                    "Create",
                    new { bookingId = booking.Id });
            }

            // Tạo mã thanh toán
            var paymentCode =
                "PAY" + DateTime.Now.ToString("yyyyMMddHHmmssfff");

            var payment = new Models.Payment
            {
                PaymentCode = paymentCode,
                UserId = userId,
                BookingId = booking.Id,
                Amount = booking.TotalPrice,
                PaymentMethod = paymentMethod,
                Status = "Pending",
                PaymentDate = DateTime.Now,
                Note = note
            };

            _context.Payments.Add(payment);

            // Lưu phương thức thanh toán vào Booking
            booking.PaymentMethod = paymentMethod;

            // Chưa chuyển thành Paid.
            // Admin sẽ xác nhận thanh toán.
            booking.PaymentStatus = "Unpaid";

            await _context.SaveChangesAsync();

            return RedirectToAction(
                nameof(Success),
                new { id = payment.Id });
        }
        // ================= THANH TOÁN HOTEL =================

        [HttpGet]
        public async Task<IActionResult> CreateHotel(int? hotelBookingId)
        {
            if (hotelBookingId == null)
            {
                return NotFound();
            }

            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdString, out var userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var booking = await _context.HotelBookings
                .Include(hb => hb.Hotel)
                .Include(hb => hb.Room)
                .FirstOrDefaultAsync(hb =>
                    hb.Id == hotelBookingId &&
                    hb.UserId == userId);

            if (booking == null)
            {
                return NotFound();
            }

            // Không cho thanh toán đơn đã hủy
            if (booking.Status == "Cancelled")
            {
                TempData["Error"] = "Đơn đặt phòng này đã bị hủy.";

                return RedirectToAction(
                    "BookingDetails",
                    "Hotel",
                    new { id = booking.Id });
            }

            // Không cho thanh toán lại
            if (booking.PaymentStatus == "Paid")
            {
                TempData["Error"] = "Đơn đặt phòng này đã được thanh toán.";

                return RedirectToAction(
                    "BookingDetails",
                    "Hotel",
                    new { id = booking.Id });
            }

            // Kiểm tra đã có giao dịch đang chờ xử lý chưa
            var existingPayment = await _context.Payments
                .FirstOrDefaultAsync(p =>
                    p.HotelBookingId == booking.Id &&
                    p.Status == "Pending");

            if (existingPayment != null)
            {
                TempData["Error"] =
                    "Đơn phòng này đã có một giao dịch thanh toán đang chờ xử lý.";

                return RedirectToAction(
                    "BookingDetails",
                    "Hotel",
                    new { id = booking.Id });
            }

            return View(booking);
        }
        // Tạo giao dịch thanh toán Hotel
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateHotel(
            int hotelBookingId,
            string paymentMethod,
            string? note)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdString, out var userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var booking = await _context.HotelBookings
                .FirstOrDefaultAsync(hb =>
                    hb.Id == hotelBookingId &&
                    hb.UserId == userId);

            if (booking == null)
            {
                return NotFound();
            }

            // Không cho thanh toán đơn đã hủy
            if (booking.Status == "Cancelled")
            {
                TempData["Error"] = "Đơn đặt phòng này đã bị hủy.";

                return RedirectToAction(
                    "BookingDetails",
                    "Hotel",
                    new { id = booking.Id });
            }

            // Không cho thanh toán lại
            if (booking.PaymentStatus == "Paid")
            {
                TempData["Error"] = "Đơn đặt phòng này đã được thanh toán.";

                return RedirectToAction(
                    "BookingDetails",
                    "Hotel",
                    new { id = booking.Id });
            }

            // Kiểm tra giao dịch đang chờ xử lý
            var existingPayment = await _context.Payments
                .FirstOrDefaultAsync(p =>
                    p.HotelBookingId == booking.Id &&
                    p.Status == "Pending");

            if (existingPayment != null)
            {
                TempData["Error"] =
                    "Đơn phòng này đã có một giao dịch thanh toán đang chờ xử lý.";

                return RedirectToAction(
                    "BookingDetails",
                    "Hotel",
                    new { id = booking.Id });
            }

            if (string.IsNullOrWhiteSpace(paymentMethod))
            {
                TempData["Error"] =
                    "Vui lòng chọn phương thức thanh toán.";

                return RedirectToAction(
                    "CreateHotel",
                    new { hotelBookingId = booking.Id });
            }

            // Tạo mã thanh toán
            var paymentCode =
                "PAY" + DateTime.Now.ToString("yyyyMMddHHmmssfff");

            var payment = new Models.Payment
            {
                PaymentCode = paymentCode,
                UserId = userId,
                HotelBookingId = booking.Id,
                Amount = booking.TotalPrice,
                PaymentMethod = paymentMethod,
                Status = "Pending",
                PaymentDate = DateTime.Now,
                Note = note
            };

            _context.Payments.Add(payment);

            // Lưu phương thức thanh toán vào HotelBooking
            booking.PaymentMethod = paymentMethod;

            // Chưa chuyển sang Paid.
            // Admin sẽ xác nhận thanh toán.
            booking.PaymentStatus = "Unpaid";

            await _context.SaveChangesAsync();

            return RedirectToAction(
                nameof(Success),
                new { id = payment.Id });
        }

        // Trang thông báo đã tạo giao dịch
        [HttpGet]
        public async Task<IActionResult> Success(int id)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdString, out var userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var payment = await _context.Payments
                .Include(p => p.Booking)
                    .ThenInclude(b => b!.Tour)
                .Include(p => p.HotelBooking)
                    .ThenInclude(hb => hb!.Hotel)
                .Include(p => p.HotelBooking)
                    .ThenInclude(hb => hb!.Room)
                .FirstOrDefaultAsync(p =>
                    p.Id == id &&
                    p.UserId == userId);

            if (payment == null)
            {
                return NotFound();
            }

            return View(payment);
        }
    }
}