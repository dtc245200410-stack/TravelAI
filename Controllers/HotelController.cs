using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using TravelAI.Data;

namespace TravelAI.Controllers
{
    public class HotelController : Controller
    {
        private readonly TravelDbContext _context;

        public HotelController(TravelDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(
    string? searchString,
    string? city,
    string? sort,
    int page = 1)
        {
            const int pageSize = 9;

            if (page < 1)
            {
                page = 1;
            }

            var hotels = _context.Hotels
                .Include(h => h.Destination)
                .Where(h => h.Status == "Active")
                .AsQueryable();

            // ================= TÌM KIẾM =================

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                hotels = hotels.Where(h =>
                    h.Name.Contains(searchString) ||
                    h.Description.Contains(searchString) ||
                    h.Address.Contains(searchString) ||
                    h.City.Contains(searchString) ||
                    h.Province.Contains(searchString));
            }

            // ================= LỌC THÀNH PHỐ =================

            if (!string.IsNullOrWhiteSpace(city))
            {
                hotels = hotels.Where(h =>
                    h.City == city);
            }

            // ================= SẮP XẾP =================

            hotels = sort switch
            {
                "rating" =>
                    hotels.OrderByDescending(h => h.Rating),

                "nameAsc" =>
                    hotels.OrderBy(h => h.Name),

                "nameDesc" =>
                    hotels.OrderByDescending(h => h.Name),

                "priceAsc" =>
                    hotels.OrderBy(h =>
                        h.Rooms
                            .Select(r => r.PricePerNight)
                            .DefaultIfEmpty(0)
                            .Min()),

                "priceDesc" =>
                    hotels.OrderByDescending(h =>
                        h.Rooms
                            .Select(r => r.PricePerNight)
                            .DefaultIfEmpty(0)
                            .Min()),

                _ =>
                    hotels.OrderByDescending(h => h.Rating)
            };

            // ================= PHÂN TRANG =================

            var totalHotels = await hotels.CountAsync();

            var totalPages = (int)Math.Ceiling(
                totalHotels / (double)pageSize);

            if (totalPages > 0 && page > totalPages)
            {
                page = totalPages;
            }

            var result = await hotels
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // ================= DANH SÁCH THÀNH PHỐ =================

            var cities = await _context.Hotels
                .Where(h => h.Status == "Active")
                .Select(h => h.City)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            // ================= VIEWBAG =================

            ViewBag.Cities = cities;
            ViewBag.SearchString = searchString;
            ViewBag.SelectedCity = city;
            ViewBag.SelectedSort = sort;

            ViewBag.TotalHotels = totalHotels;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            return View(result);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var hotel = await _context.Hotels
                .Include(h => h.Destination)
                .Include(h => h.Rooms)
                .FirstOrDefaultAsync(h => h.Id == id);

            if (hotel == null)
            {
                return NotFound();
            }

            return View(hotel);
        }
        // ================= ĐẶT PHÒNG =================

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Create(int? hotelId, int? roomId)
        {
            if (hotelId == null || roomId == null)
            {
                return NotFound();
            }

            var hotel = await _context.Hotels
                .Include(h => h.Rooms)
                .FirstOrDefaultAsync(h => h.Id == hotelId);

            if (hotel == null)
            {
                return NotFound();
            }

            var room = hotel.Rooms
                .FirstOrDefault(r => r.Id == roomId);

            if (room == null)
            {
                return NotFound();
            }

            ViewBag.Hotel = hotel;
            ViewBag.Room = room;

            return View();
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            int hotelId,
            int roomId,
            DateTime checkInDate,
            DateTime checkOutDate,
            int numberOfGuests,
            int numberOfRooms,
            string? note)
        {
            var hotel = await _context.Hotels
                .FirstOrDefaultAsync(h => h.Id == hotelId);

            if (hotel == null)
            {
                return NotFound();
            }

            var room = await _context.Rooms
                .FirstOrDefaultAsync(r =>
                    r.Id == roomId &&
                    r.HotelId == hotelId);

            if (room == null)
            {
                return NotFound();
            }

            // Kiểm tra ngày
            if (checkInDate.Date < DateTime.Today)
            {
                ModelState.AddModelError(
                    "checkInDate",
                    "Ngày nhận phòng không được ở quá khứ.");
            }

            if (checkOutDate.Date <= checkInDate.Date)
            {
                ModelState.AddModelError(
                    "checkOutDate",
                    "Ngày trả phòng phải sau ngày nhận phòng.");
            }

            // Kiểm tra số khách
            if (numberOfGuests < 1)
            {
                ModelState.AddModelError(
                    "numberOfGuests",
                    "Số khách phải lớn hơn 0.");
            }

            if (numberOfGuests > room.MaxGuests * numberOfRooms)
            {
                ModelState.AddModelError(
                    "numberOfGuests",
                    $"Phòng này chỉ phục vụ tối đa {room.MaxGuests} khách/phòng.");
            }

            // Kiểm tra số phòng
            if (numberOfRooms < 1)
            {
                ModelState.AddModelError(
                    "numberOfRooms",
                    "Số phòng phải lớn hơn 0.");
            }

            if (numberOfRooms > room.AvailableQuantity)
            {
                ModelState.AddModelError(
                    "numberOfRooms",
                    $"Hiện chỉ còn {room.AvailableQuantity} phòng.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Hotel = hotel;
                ViewBag.Room = room;

                return View();
            }

            // Tính số đêm
            var numberOfNights =
                (checkOutDate.Date - checkInDate.Date).Days;

            // Tính tổng tiền
            var totalPrice =
                room.PricePerNight *
                numberOfNights *
                numberOfRooms;

            // Lấy thông tin người dùng từ Claims
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

            // Tạo mã booking
            var bookingCode =
                "HB" + DateTime.Now.ToString("yyyyMMddHHmmss");

            var booking = new Models.HotelBooking
            {
                BookingCode = bookingCode,

                UserId = user.Id,

                HotelId = hotel.Id,

                RoomId = room.Id,

                CheckInDate = checkInDate.Date,

                CheckOutDate = checkOutDate.Date,

                NumberOfGuests = numberOfGuests,

                NumberOfRooms = numberOfRooms,

                TotalPrice = totalPrice,

                Status = "Pending",

                PaymentStatus = "Unpaid",

                PaymentMethod = null,

                BookingDate = DateTime.Now,

                Note = note
            };

            _context.HotelBookings.Add(booking);

            // Trừ số phòng còn lại
            room.AvailableQuantity -= numberOfRooms;

            await _context.SaveChangesAsync();

            return RedirectToAction(
                "BookingDetails",
                new { id = booking.Id });
        }
        // ================= CHI TIẾT ĐẶT PHÒNG =================

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> BookingDetails(int? id)
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

            var booking = await _context.HotelBookings
                .Include(hb => hb.Hotel)
                .Include(hb => hb.Room)
                .FirstOrDefaultAsync(hb =>
                    hb.Id == id &&
                    hb.UserId == userId);

            if (booking == null)
            {
                return NotFound();
            }

            return View(booking);
        }
        // ================= HỦY ĐẶT PHÒNG =================

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelBooking(int id)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdString, out var userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var booking = await _context.HotelBookings
                .Include(hb => hb.Room)
                .FirstOrDefaultAsync(hb =>
                    hb.Id == id &&
                    hb.UserId == userId);

            if (booking == null)
            {
                return NotFound();
            }

            if (booking.Status == "Cancelled")
            {
                return BadRequest("Đơn phòng này đã được hủy.");
            }

            // Hoàn lại số phòng
            if (booking.Room != null)
            {
                booking.Room.AvailableQuantity += booking.NumberOfRooms;

                if (booking.Room.AvailableQuantity > booking.Room.Quantity)
                {
                    booking.Room.AvailableQuantity = booking.Room.Quantity;
                }
            }

            // Hủy đơn
            booking.Status = "Cancelled";
            booking.CancelledAt = DateTime.Now;

            // Nếu đơn đã thanh toán thì đánh dấu giao dịch là Cancelled
            if (booking.PaymentStatus == "Paid")
            {
                var payment = await _context.Payments
                    .FirstOrDefaultAsync(p =>
                        p.HotelBookingId == booking.Id &&
                        p.Status == "Paid");

                if (payment != null)
                {
                    payment.Status = "Cancelled";
                }

                booking.PaymentStatus = "Cancelled";
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(
                "BookingDetails",
                new { id = booking.Id });
        }
    }
}