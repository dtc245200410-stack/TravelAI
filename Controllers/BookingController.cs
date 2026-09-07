using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using TravelAI.Data;

namespace TravelAI.Controllers
{
    public class BookingController : Controller
    {
        private readonly TravelDbContext _context;

        public BookingController(TravelDbContext context)
        {
            _context = context;
        }
        [Authorize]
        public async Task<IActionResult> Index()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdString, out var userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var tourBookings = await _context.Bookings
                .Include(b => b.Tour)
                .ThenInclude(t => t!.Destination)
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();

            var hotelBookings = await _context.HotelBookings
                .Include(hb => hb.Hotel)
                .Include(hb => hb.Room)
                .Where(hb => hb.UserId == userId)
                .OrderByDescending(hb => hb.BookingDate)
                .ToListAsync();

            var model = new Models.ViewModels.MyBookingViewModel
            {
                TourBookings = tourBookings,
                HotelBookings = hotelBookings
            };

            return View(model);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Create(int? tourId)
        {
            if (tourId == null)
            {
                return NotFound();
            }

            var tour = await _context.Tours
                .Include(t => t.Destination)
                .FirstOrDefaultAsync(t => t.Id == tourId);

            if (tour == null)
            {
                return NotFound();
            }

            return View(tour);
        }
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            int tourId,
            int numberOfPeople,
            string? note)
        {
            // Tìm tour
            var tour = await _context.Tours
                .FirstOrDefaultAsync(t => t.Id == tourId);

            if (tour == null)
            {
                return NotFound();
            }

            // Kiểm tra số người
            if (numberOfPeople < 1)
            {
                ModelState.AddModelError(
                    "numberOfPeople",
                    "Số người phải lớn hơn 0.");
            }

            if (numberOfPeople > tour.AvailablePeople)
            {
                ModelState.AddModelError(
                    "numberOfPeople",
                    $"Tour chỉ còn {tour.AvailablePeople} chỗ.");
            }

            // Nếu dữ liệu không hợp lệ
            if (!ModelState.IsValid)
            {
                return View(tour);
            }

            // Tính giá ở SERVER
            // Không lấy tổng tiền từ JavaScript để tránh sửa giá phía trình duyệt
            var price = tour.SalePrice ?? tour.Price;
            var totalPrice = price * numberOfPeople;

            // Tạo mã booking
            var bookingCode =
                "BK" + DateTime.Now.ToString("yyyyMMddHHmmss");

            // Lấy userId từ claims
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdString, out var userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }
            // Tạo booking
            var booking = new Models.Booking
            {
                BookingCode = bookingCode,
                UserId = user.Id,
                TourId = tour.Id,
                NumberOfPeople = numberOfPeople,
                TotalPrice = totalPrice,
                Status = "Pending",
                PaymentStatus = "Unpaid",
                PaymentMethod = null,
                BookingDate = DateTime.Now,
                Note = note
            };

            // Lưu database
            _context.Bookings.Add(booking);

            // Cập nhật số chỗ còn lại
            tour.AvailablePeople -= numberOfPeople;

            await _context.SaveChangesAsync();

            return RedirectToAction(
                "Details",
                new { id = booking.Id });
        }
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
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
                .ThenInclude(t => t!.Destination)
                .FirstOrDefaultAsync(b =>
                    b.Id == id &&
                    b.UserId == userId);

            if (booking == null)
            {
                return NotFound();
            }

            return View(booking);
        }
    }
}