using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelAI.Data;
using TravelAI.Models;

namespace TravelAI.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminRoomController : Controller
    {
        private readonly TravelDbContext _context;

        public AdminRoomController(TravelDbContext context)
        {
            _context = context;
        }

        // =========================
        // DANH SÁCH PHÒNG
        // =========================
        public async Task<IActionResult> Index(
            string? searchString,
            string? status,
            int? hotelId)
        {
            var rooms = _context.Rooms
                .Include(r => r.Hotel)
                .AsQueryable();

            // Tìm kiếm
            if (!string.IsNullOrWhiteSpace(searchString))
            {
                searchString = searchString.Trim();

                rooms = rooms.Where(r =>
                    r.RoomName.Contains(searchString) ||
                    (r.RoomType != null &&
                     r.RoomType.Contains(searchString)) ||
                    (r.Hotel != null &&
                     r.Hotel.Name.Contains(searchString)));
            }

            // Lọc trạng thái
            if (!string.IsNullOrWhiteSpace(status) &&
                status != "All")
            {
                rooms = rooms.Where(r => r.Status == status);
            }

            // Lọc theo khách sạn
            if (hotelId.HasValue && hotelId.Value > 0)
            {
                rooms = rooms.Where(r => r.HotelId == hotelId.Value);
            }

            var result = await rooms
                .OrderBy(r => r.Hotel!.Name)
                .ThenBy(r => r.RoomName)
                .ToListAsync();

            // Danh sách khách sạn cho bộ lọc
            ViewBag.Hotels = await _context.Hotels
                .OrderBy(h => h.Name)
                .ToListAsync();

            ViewBag.SearchString = searchString;
            ViewBag.Status = status ?? "All";
            ViewBag.HotelId = hotelId;

            return View(result);
        }

        // =========================
        // CHI TIẾT PHÒNG
        // =========================
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var room = await _context.Rooms
                .Include(r => r.Hotel)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (room == null)
            {
                return NotFound();
            }

            return View(room);
        }

        // =========================
        // GET: THÊM PHÒNG
        // =========================
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.Hotels = await _context.Hotels
                .Where(h => h.Status == "Active")
                .OrderBy(h => h.Name)
                .ToListAsync();

            return View();
        }

        // =========================
        // POST: THÊM PHÒNG
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Room room)
        {
            room.RoomName = room.RoomName?.Trim() ?? string.Empty;
            room.RoomType = room.RoomType?.Trim();
            room.Description = room.Description?.Trim();
            room.ImageUrl = room.ImageUrl?.Trim();

            var hotelExists = await _context.Hotels
                .AnyAsync(h => h.Id == room.HotelId);

            if (!hotelExists)
            {
                ModelState.AddModelError(
                    "HotelId",
                    "Khách sạn không tồn tại.");
            }

            if (room.Quantity < 0)
            {
                ModelState.AddModelError(
                    "Quantity",
                    "Số lượng phòng không được âm.");
            }

            if (room.AvailableQuantity < 0)
            {
                ModelState.AddModelError(
                    "AvailableQuantity",
                    "Số phòng còn trống không được âm.");
            }

            if (room.AvailableQuantity > room.Quantity)
            {
                ModelState.AddModelError(
                    "AvailableQuantity",
                    "Số phòng còn trống không được lớn hơn tổng số phòng.");
            }

            if (string.IsNullOrWhiteSpace(room.Status))
            {
                room.Status = "Available";
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Hotels = await _context.Hotels
                    .Where(h => h.Status == "Active")
                    .OrderBy(h => h.Name)
                    .ToListAsync();

                return View(room);
            }

            _context.Rooms.Add(room);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Thêm phòng thành công.";

            return RedirectToAction(nameof(Index));
        }

        // =========================
        // GET: SỬA PHÒNG
        // =========================
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var room = await _context.Rooms
                .FirstOrDefaultAsync(r => r.Id == id);

            if (room == null)
            {
                return NotFound();
            }

            ViewBag.Hotels = await _context.Hotels
                .OrderBy(h => h.Name)
                .ToListAsync();

            return View(room);
        }

        // =========================
        // POST: SỬA PHÒNG
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            Room room)
        {
            if (id != room.Id)
            {
                return NotFound();
            }

            var existingRoom = await _context.Rooms
                .FirstOrDefaultAsync(r => r.Id == id);

            if (existingRoom == null)
            {
                return NotFound();
            }

            room.RoomName = room.RoomName?.Trim() ?? string.Empty;
            room.RoomType = room.RoomType?.Trim();
            room.Description = room.Description?.Trim();
            room.ImageUrl = room.ImageUrl?.Trim();

            var hotelExists = await _context.Hotels
                .AnyAsync(h => h.Id == room.HotelId);

            if (!hotelExists)
            {
                ModelState.AddModelError(
                    "HotelId",
                    "Khách sạn không tồn tại.");
            }

            if (room.Quantity < 0)
            {
                ModelState.AddModelError(
                    "Quantity",
                    "Số lượng phòng không được âm.");
            }

            if (room.AvailableQuantity < 0)
            {
                ModelState.AddModelError(
                    "AvailableQuantity",
                    "Số phòng còn trống không được âm.");
            }

            if (room.AvailableQuantity > room.Quantity)
            {
                ModelState.AddModelError(
                    "AvailableQuantity",
                    "Số phòng còn trống không được lớn hơn tổng số phòng.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Hotels = await _context.Hotels
                    .OrderBy(h => h.Name)
                    .ToListAsync();

                return View(room);
            }

            existingRoom.RoomName = room.RoomName;
            existingRoom.RoomType = room.RoomType;
            existingRoom.MaxGuests = room.MaxGuests;
            existingRoom.PricePerNight = room.PricePerNight;
            existingRoom.Quantity = room.Quantity;
            existingRoom.AvailableQuantity = room.AvailableQuantity;
            existingRoom.Description = room.Description;
            existingRoom.ImageUrl = room.ImageUrl;
            existingRoom.Status = room.Status;
            existingRoom.HotelId = room.HotelId;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Cập nhật phòng thành công.";

            return RedirectToAction(nameof(Index));
        }

        // =========================
        // GET: XÓA PHÒNG
        // =========================
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var room = await _context.Rooms
                .Include(r => r.Hotel)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (room == null)
            {
                return NotFound();
            }

            return View(room);
        }

        // =========================
        // POST: XÁC NHẬN XÓA
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var room = await _context.Rooms
                .FirstOrDefaultAsync(r => r.Id == id);

            if (room == null)
            {
                return NotFound();
            }

            _context.Rooms.Remove(room);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Xóa phòng thành công.";

            return RedirectToAction(nameof(Index));
        }

        // =========================
        // ĐỔI TRẠNG THÁI
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeStatus(
            int id,
            string status)
        {
            var room = await _context.Rooms
                .FirstOrDefaultAsync(r => r.Id == id);

            if (room == null)
            {
                return NotFound();
            }

            var allowedStatuses = new[]
            {
                "Available",
                "Unavailable"
            };

            if (!allowedStatuses.Contains(status))
            {
                TempData["Error"] = "Trạng thái không hợp lệ.";

                return RedirectToAction(nameof(Index));
            }

            room.Status = status;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                status == "Available"
                    ? "Đã chuyển phòng sang trạng thái đang hoạt động."
                    : "Đã tạm ngưng phòng.";

            return RedirectToAction(nameof(Index));
        }
    }
}