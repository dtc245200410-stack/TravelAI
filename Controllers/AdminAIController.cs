using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using TravelAI.Data;
using TravelAI.Services;

namespace TravelAI.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminAIController : Controller
    {
        private readonly TravelDbContext _context;
        private readonly OllamaService _ollamaService;

        public AdminAIController(
            TravelDbContext context,
            OllamaService ollamaService)
        {
            _context = context;
            _ollamaService = ollamaService;
        }

        public async Task<IActionResult> Index()
        {
            var conversations = await _context.AIConversations
                .Include(x => x.User)
                .Include(x => x.Messages)
                .OrderByDescending(x => x.UpdatedAt)
                .ToListAsync();

            return View(conversations);
        }

        [HttpPost]
        public async Task<IActionResult> Chat(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return Json(new
                {
                    success = false,
                    message = "Vui lòng nhập câu hỏi."
                });
            }

            try
            {
                // =========================
                // 1. LẤY DỮ LIỆU HỆ THỐNG
                // =========================

                int userCount =
                    await _context.Users.CountAsync();

                int tourCount =
                    await _context.Tours.CountAsync();

                int hotelCount =
                    await _context.Hotels.CountAsync();

                int roomCount =
                    await _context.Rooms.CountAsync();

                int bookingCount =
                    await _context.Bookings.CountAsync();

                int hotelBookingCount =
                    await _context.HotelBookings.CountAsync();

                // =========================
                // 2. THỐNG KÊ BOOKING
                // =========================

                int pendingBookings =
                    await _context.Bookings
                        .CountAsync(x => x.Status == "Pending");

                int confirmedBookings =
                    await _context.Bookings
                        .CountAsync(x => x.Status == "Confirmed");

                int cancelledBookings =
                    await _context.Bookings
                        .CountAsync(x => x.Status == "Cancelled");

                // =========================
                // 3. TÍNH DOANH THU
                // =========================

                decimal totalRevenue =
                    await _context.Bookings
                        .Where(x => x.Status == "Confirmed")
                        .SumAsync(x => (decimal?)x.TotalPrice)
                        ?? 0;

                // =========================
                // 4. TẠO DỮ LIỆU CHO AI
                // =========================

                var systemData = new StringBuilder();

                systemData.AppendLine(
                    "DỮ LIỆU HỆ THỐNG TRAVELAI:"
                );

                systemData.AppendLine(
                    $"Số tài khoản: {userCount}"
                );

                systemData.AppendLine(
                    $"Số tour: {tourCount}"
                );

                systemData.AppendLine(
                    $"Số khách sạn: {hotelCount}"
                );

                systemData.AppendLine(
                    $"Số phòng: {roomCount}"
                );

                systemData.AppendLine(
                    $"Số đơn đặt tour: {bookingCount}"
                );

                systemData.AppendLine(
                    $"Số đơn đặt khách sạn: {hotelBookingCount}"
                );

                systemData.AppendLine(
                    $"Đơn chờ xác nhận: {pendingBookings}"
                );

                systemData.AppendLine(
                    $"Đơn đã xác nhận: {confirmedBookings}"
                );

                systemData.AppendLine(
                    $"Đơn đã hủy: {cancelledBookings}"
                );

                systemData.AppendLine(
                    $"Tổng doanh thu từ đơn đã xác nhận: {totalRevenue:N0} VNĐ"
                );

                // =========================
                // 5. PROMPT CHO ADMIN AI
                // =========================

                string prompt = $"""
Bạn là AI trợ lý quản lý của hệ thống TravelAI.

Bạn đang hỗ trợ người quản trị hệ thống.

Dưới đây là dữ liệu THẬT lấy trực tiếp từ database:

{systemData}

Câu hỏi của quản trị viên:

{message}

Yêu cầu:

1. Chỉ sử dụng dữ liệu được cung cấp ở trên.
2. Không được tự bịa thêm số liệu.
3. Nếu dữ liệu chưa đủ để trả lời thì nói rõ:
   "Dữ liệu hiện tại chưa đủ để trả lời câu hỏi này."
4. Trả lời bằng tiếng Việt.
5. Trình bày rõ ràng, dễ đọc.
6. Nếu câu hỏi liên quan đến doanh thu thì sử dụng doanh thu từ các đơn đã xác nhận.
7. Nếu người quản trị hỏi thống kê, hãy đưa ra số liệu cụ thể.
8. Nếu có thể, đưa ra nhận xét ngắn dựa trên số liệu nhưng không được bịa thêm dữ liệu.
""";

                // =========================
                // 6. GỌI OLLAMA
                // =========================

                string aiResponse =
                    await _ollamaService.AskAsync(prompt);

                return Json(new
                {
                    success = true,
                    message = aiResponse
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Lỗi AI: " + ex.Message
                });
            }
        }
    }
}