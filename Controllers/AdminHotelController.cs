
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelAI.Data;
using TravelAI.Models;

namespace TravelAI.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminHotelController : Controller
    {
        private readonly TravelDbContext _context;

        public AdminHotelController(TravelDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // DANH SÁCH HOTEL
        // =========================================================

        public async Task<IActionResult> Index(
            string? searchString,
            string? status)
        {
            var hotels = _context.Hotels
                .Include(h => h.Destination)
                .Include(h => h.Rooms)
                .AsQueryable();

            // Tìm kiếm
            if (!string.IsNullOrWhiteSpace(searchString))
            {
                searchString = searchString.Trim();

                hotels = hotels.Where(h =>
                    h.Name.Contains(searchString) ||
                    h.Address.Contains(searchString) ||
                    (h.City != null && h.City.Contains(searchString)) ||
                    (h.Province != null && h.Province.Contains(searchString)));
            }

            // Lọc trạng thái
            if (!string.IsNullOrWhiteSpace(status) &&
                status != "All")
            {
                hotels = hotels.Where(h => h.Status == status);
            }

            var result = await hotels
                .OrderByDescending(h => h.CreatedAt)
                .ToListAsync();

            ViewBag.SearchString = searchString;
            ViewBag.Status = status ?? "All";

            return View(result);
        }


        // =========================================================
        // CHI TIẾT HOTEL
        // =========================================================

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var hotel = await _context.Hotels
                .Include(h => h.Destination)
                .Include(h => h.Rooms)
                .Include(h => h.HotelBookings)
                .FirstOrDefaultAsync(h => h.Id == id);

            if (hotel == null)
            {
                return NotFound();
            }

            return View(hotel);
        }


        // =========================================================
        // TẠO HOTEL - GET
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.Destinations = await _context.Destinations
                .OrderBy(d => d.Name)
                .ToListAsync();

            return View();
        }


        // =========================================================
        // TẠO HOTEL - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Hotel hotel)
        {
            // Kiểm tra Destination
            var destinationExists = await _context.Destinations
                .AnyAsync(d => d.Id == hotel.DestinationId);

            if (!destinationExists)
            {
                ModelState.AddModelError(
                    "DestinationId",
                    "Điểm đến không tồn tại.");
            }

            // Chuẩn hóa dữ liệu
            hotel.Name = hotel.Name?.Trim() ?? string.Empty;
            hotel.Address = hotel.Address?.Trim() ?? string.Empty;
            hotel.City = hotel.City?.Trim();
            hotel.Province = hotel.Province?.Trim();
            hotel.Slug = hotel.Slug?.Trim();
            hotel.ImageUrl = hotel.ImageUrl?.Trim();

            // Giá trị mặc định
            if (hotel.CreatedAt == default)
            {
                hotel.CreatedAt = DateTime.Now;
            }

            if (string.IsNullOrWhiteSpace(hotel.Status))
            {
                hotel.Status = "Active";
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Destinations = await _context.Destinations
                    .OrderBy(d => d.Name)
                    .ToListAsync();

                return View(hotel);
            }

            _context.Hotels.Add(hotel);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Thêm khách sạn thành công.";

            return RedirectToAction(nameof(Index));
        }


        // =========================================================
        // SỬA HOTEL - GET
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var hotel = await _context.Hotels
                .FirstOrDefaultAsync(h => h.Id == id);

            if (hotel == null)
            {
                return NotFound();
            }

            ViewBag.Destinations = await _context.Destinations
                .OrderBy(d => d.Name)
                .ToListAsync();

            return View(hotel);
        }


        // =========================================================
        // SỬA HOTEL - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            Hotel hotel)
        {
            if (id != hotel.Id)
            {
                return NotFound();
            }

            var existingHotel = await _context.Hotels
                .FirstOrDefaultAsync(h => h.Id == id);

            if (existingHotel == null)
            {
                return NotFound();
            }

            // Kiểm tra Destination
            var destinationExists = await _context.Destinations
                .AnyAsync(d => d.Id == hotel.DestinationId);

            if (!destinationExists)
            {
                ModelState.AddModelError(
                    "DestinationId",
                    "Điểm đến không tồn tại.");
            }

            // Chuẩn hóa dữ liệu
            hotel.Name = hotel.Name?.Trim() ?? string.Empty;
            hotel.Address = hotel.Address?.Trim() ?? string.Empty;
            hotel.City = hotel.City?.Trim();
            hotel.Province = hotel.Province?.Trim();
            hotel.Slug = hotel.Slug?.Trim();
            hotel.ImageUrl = hotel.ImageUrl?.Trim();

            if (!ModelState.IsValid)
            {
                ViewBag.Destinations = await _context.Destinations
                    .OrderBy(d => d.Name)
                    .ToListAsync();

                return View(hotel);
            }

            // Cập nhật từng trường cần thiết
            existingHotel.Name = hotel.Name;
            existingHotel.Slug = hotel.Slug;
            existingHotel.Description = hotel.Description;
            existingHotel.Address = hotel.Address;
            existingHotel.City = hotel.City;
            existingHotel.Province = hotel.Province;
            existingHotel.ImageUrl = hotel.ImageUrl;
            existingHotel.Rating = hotel.Rating;
            existingHotel.ReviewCount = hotel.ReviewCount;
            existingHotel.Status = hotel.Status;
            existingHotel.DestinationId = hotel.DestinationId;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Cập nhật khách sạn thành công.";

            return RedirectToAction(nameof(Index));
        }


        // =========================================================
        // XÓA HOTEL - GET
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var hotel = await _context.Hotels
                .Include(h => h.Destination)
                .Include(h => h.Rooms)
                .Include(h => h.HotelBookings)
                .FirstOrDefaultAsync(h => h.Id == id);

            if (hotel == null)
            {
                return NotFound();
            }

            return View(hotel);
        }


        // =========================================================
        // XÓA HOTEL - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var hotel = await _context.Hotels
                .Include(h => h.Rooms)
                .Include(h => h.HotelBookings)
                .FirstOrDefaultAsync(h => h.Id == id);

            if (hotel == null)
            {
                return NotFound();
            }

            // Không cho xóa Hotel đang có Booking
            if (hotel.HotelBookings.Any())
            {
                TempData["Error"] =
                    "Không thể xóa khách sạn vì khách sạn đã có Booking.";

                return RedirectToAction(nameof(Index));
            }

            // Không cho xóa Hotel còn phòng
            if (hotel.Rooms.Any())
            {
                TempData["Error"] =
                    "Không thể xóa khách sạn vì vẫn còn phòng thuộc khách sạn. " +
                    "Hãy xóa các phòng trước.";

                return RedirectToAction(nameof(Index));
            }

            _context.Hotels.Remove(hotel);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Xóa khách sạn thành công.";

            return RedirectToAction(nameof(Index));
        }


        // =========================================================
        // THAY ĐỔI TRẠNG THÁI HOTEL
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeStatus(
            int id,
            string status)
        {
            var hotel = await _context.Hotels
                .FirstOrDefaultAsync(h => h.Id == id);

            if (hotel == null)
            {
                return NotFound();
            }

            var allowedStatuses = new[]
            {
                "Active",
                "Inactive"
            };

            if (!allowedStatuses.Contains(status))
            {
                TempData["Error"] = "Trạng thái không hợp lệ.";

                return RedirectToAction(nameof(Index));
            }

            hotel.Status = status;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                status == "Active"
                    ? "Đã kích hoạt khách sạn."
                    : "Đã tạm ngưng khách sạn.";

            return RedirectToAction(nameof(Index));
        }
    }
}