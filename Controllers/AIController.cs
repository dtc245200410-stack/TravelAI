using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using TravelAI.Data;
using TravelAI.Models;
using TravelAI.Services;
using TravelAI.ViewModels;

namespace TravelAI.Controllers
{
    [Authorize]
    public class AIController : Controller
    {
        private readonly TravelDbContext _context;
        private readonly OllamaService _ollamaService;

        public AIController(
            TravelDbContext context,
            OllamaService ollamaService)
        {
            _context = context;
            _ollamaService = ollamaService;
        }

        // =====================================================
        // 1. TRANG CHAT AI
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Index(int? conversationId)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdString, out int userId))
            {
                return RedirectToAction("Login", "Account");
            }

            // Lấy toàn bộ lịch sử hội thoại của người dùng
            var conversations = await _context.AIConversations
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.UpdatedAt)
                .ToListAsync();

            AIConversation? currentConversation = null;

            // Nếu người dùng chọn một cuộc hội thoại cụ thể
            if (conversationId.HasValue)
            {
                currentConversation = conversations
                    .FirstOrDefault(c => c.Id == conversationId.Value);
            }

            // Nếu không chọn conversation nào
            // thì tự mở cuộc trò chuyện gần nhất
            if (currentConversation == null && conversations.Any())
            {
                currentConversation = conversations.First();
            }

            // Nếu có cuộc trò chuyện hiện tại
            // thì lấy toàn bộ tin nhắn của cuộc trò chuyện đó
            if (currentConversation != null)
            {
                currentConversation.Messages = await _context.AIChatMessages
                    .Where(m => m.ConversationId == currentConversation.Id)
                    .OrderBy(m => m.CreatedAt)
                    .ToListAsync();
            }

            var model = new AIChatViewModel
            {
                CurrentConversation = currentConversation,
                Conversations = conversations
            };

            return View(model);
        }

        // =====================================================
        // 2. CHAT
        // =====================================================

        [HttpPost]
        public async Task<IActionResult> Chat(
            int? conversationId,
            string message)
        {
            // -------------------------------------------------
            // Kiểm tra message
            // -------------------------------------------------

            if (string.IsNullOrWhiteSpace(message))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Vui lòng nhập nội dung cần tư vấn."
                });
            }

            // -------------------------------------------------
            // Lấy User hiện tại
            // -------------------------------------------------

            var userIdString = User.FindFirstValue(
                ClaimTypes.NameIdentifier
            );

            if (!int.TryParse(userIdString, out var userId))
            {
                return Unauthorized();
            }

            // -------------------------------------------------
            // Lấy conversation hiện tại
            // -------------------------------------------------

            AIConversation? conversation = null;

            if (conversationId.HasValue)
            {
                conversation = await _context.AIConversations
                    .Include(c => c.Messages)
                    .FirstOrDefaultAsync(c =>
                        c.Id == conversationId.Value &&
                        c.UserId == userId
                    );
            }

            // Nếu chưa có conversation thì tạo mới
            if (conversation == null)
            {
                conversation = new AIConversation
                {
                    UserId = userId,

                    Title = message.Length > 200
                        ? message.Substring(0, 200)
                        : message,

                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                _context.AIConversations.Add(conversation);

                await _context.SaveChangesAsync();
            }

            // =================================================
            // 3. LƯU CÂU HỎI CỦA USER
            // =================================================

            var userMessage = new AIChatMessage
            {
                ConversationId = conversation.Id,
                Role = "User",
                Content = message.Trim(),
                CreatedAt = DateTime.Now
            };

            _context.AIChatMessages.Add(userMessage);

            await _context.SaveChangesAsync();

            // =================================================
            // 4. NHẬN DIỆN ĐIỂM ĐẾN
            // =================================================

            var destinations = await _context.Destinations
                .AsNoTracking()
                .ToListAsync();

            var destination = destinations.FirstOrDefault(d =>
                (
                    !string.IsNullOrWhiteSpace(d.Name)
                    &&
                    message.Contains(
                        d.Name,
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                ||
                (
                    !string.IsNullOrWhiteSpace(d.Slug)
                    &&
                    message.Contains(
                        d.Slug,
                        StringComparison.OrdinalIgnoreCase
                    )
                )
            );

            var destinationId = destination?.Id;

            // =================================================
            // 5. NHẬN DIỆN NGÂN SÁCH
            // =================================================

            decimal? budget = null;

            var budgetMatch = Regex.Match(
                message.ToLower(),
                @"(\d+(?:[.,]\d+)?)\s*(triệu|tr|m|k|nghìn|ngàn)"
            );

            if (budgetMatch.Success)
            {
                var numberText = budgetMatch.Groups[1].Value
                    .Replace(",", ".");

                if (decimal.TryParse(
                    numberText,
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out var number))
                {
                    var unit = budgetMatch.Groups[2].Value;

                    if (
                        unit == "triệu"
                        || unit == "tr"
                        || unit == "m"
                    )
                    {
                        budget = number * 1_000_000;
                    }
                    else
                    {
                        budget = number * 1_000;
                    }
                }
            }

            // =================================================
            // 6. NHẬN DIỆN SỐ NGƯỜI
            // =================================================

            int? numberOfPeople = null;

            var peopleMatch = Regex.Match(
                message.ToLower(),
                @"(\d+)\s*(người|nguoi|khách|khach)"
            );

            if (
                peopleMatch.Success
                &&
                int.TryParse(
                    peopleMatch.Groups[1].Value,
                    out var people)
            )
            {
                numberOfPeople = people;
            }

            // =================================================
            // 7. NHẬN DIỆN SỐ NGÀY
            // =================================================

            int? numberOfDays = null;

            var daysMatch = Regex.Match(
                message.ToLower(),
                @"(\d+)\s*(ngày|ngay)"
            );

            if (
                daysMatch.Success
                &&
                int.TryParse(
                    daysMatch.Groups[1].Value,
                    out var days)
            )
            {
                numberOfDays = days;
            }

            // =================================================
            // 8. TÌM TOUR TRONG DATABASE
            // =================================================

            var toursQuery = _context.Tours
                .Include(t => t.Destination)
                .Where(t => t.Status == "Active");

            if (destinationId.HasValue)
            {
                toursQuery = toursQuery.Where(t =>
                    t.DestinationId == destinationId.Value
                );
            }

            if (budget.HasValue)
            {
                toursQuery = toursQuery.Where(t =>
                    (t.SalePrice ?? t.Price) <= budget.Value
                );
            }

            if (numberOfDays.HasValue)
            {
                toursQuery = toursQuery.Where(t =>
                    t.Duration == numberOfDays.Value
                );
            }

            var tours = await toursQuery
                .OrderByDescending(t => t.Rating)
                .Take(5)
                .ToListAsync();

            // =================================================
            // 9. TÌM KHÁCH SẠN TRONG DATABASE
            // =================================================

            var hotelsQuery = _context.Hotels
                .Include(h => h.Destination)
                .Include(h => h.Rooms)
                .Where(h => h.Status == "Active");

            if (destinationId.HasValue)
            {
                hotelsQuery = hotelsQuery.Where(h =>
                    h.DestinationId == destinationId.Value
                );
            }

            var hotels = await hotelsQuery
                .OrderByDescending(h => h.Rating)
                .Take(5)
                .ToListAsync();

            // =================================================
            // 10. TẠO DỮ LIỆU TOUR CHO AI
            // =================================================

            var tourData = new StringBuilder();

            foreach (var tour in tours)
            {
                var price = tour.SalePrice ?? tour.Price;

                var tourUrl = Url.Action(
                    "Details",
                    "Tour",
                    new { id = tour.Id },
                    Request.Scheme
                );

                tourData.AppendLine(
                    $"- Tour: {tour.Name}"
                );

                tourData.AppendLine(
                    $"  Điểm đến: {tour.Destination?.Name ?? "Chưa xác định"}"
                );

                tourData.AppendLine(
                    $"  Giá: {price:N0} VNĐ/người"
                );

                tourData.AppendLine(
                    $"  Thời gian: {tour.Duration} ngày"
                );

                tourData.AppendLine(
                    $"  Đánh giá: {tour.Rating:0.0}/5"
                );

                tourData.AppendLine(
                    $"  Link: {tourUrl}"
                );

                tourData.AppendLine();
            }

            // =================================================
            // 11. TẠO DỮ LIỆU HOTEL CHO AI
            // =================================================

            var hotelData = new StringBuilder();

            foreach (var hotel in hotels)
            {
                var availableRoom = hotel.Rooms
                    .Where(r =>
                        r.Status == "Available"
                        &&
                        r.AvailableQuantity > 0
                    )
                    .OrderBy(r => r.PricePerNight)
                    .FirstOrDefault();

                var hotelUrl = Url.Action(
                    "Details",
                    "Hotel",
                    new { id = hotel.Id },
                    Request.Scheme
                );

                hotelData.AppendLine(
                    $"- Khách sạn: {hotel.Name}"
                );

                hotelData.AppendLine(
                    $"  Địa điểm: {hotel.City ?? hotel.Province ?? "Chưa xác định"}"
                );

                hotelData.AppendLine(
                    $"  Đánh giá: {hotel.Rating:0.0}/5"
                );

                if (availableRoom != null)
                {
                    hotelData.AppendLine(
                        $"  Phòng: {availableRoom.RoomName}"
                    );

                    hotelData.AppendLine(
                        $"  Giá phòng: {availableRoom.PricePerNight:N0} VNĐ/đêm"
                    );
                }
                else
                {
                    hotelData.AppendLine(
                        "  Phòng: Chưa có phòng trống"
                    );
                }

                hotelData.AppendLine(
                    $"  Link: {hotelUrl}"
                );

                hotelData.AppendLine();
            }

            // =================================================
            // 12. LẤY LỊCH SỬ HỘI THOẠI
            // =================================================

            var history = await _context.AIChatMessages
                .Where(m =>
                    m.ConversationId == conversation.Id
                )
                .OrderByDescending(m => m.CreatedAt)
                .Take(20)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();

            var historyText = new StringBuilder();

            foreach (var item in history)
            {
                historyText.AppendLine(
                    $"{item.Role}: {item.Content}"
                );
            }

            // =================================================
            // 13. TẠO PROMPT CHO OLLAMA
            // =================================================

            var prompt = $"""
Bạn là trợ lý du lịch AI của website TravelAI.

NHIỆM VỤ:
- Tư vấn du lịch bằng tiếng Việt.
- Dựa vào dữ liệu Tour và Khách sạn thật được cung cấp bên dưới.
- Không tự bịa Tour hoặc Khách sạn không có trong dữ liệu.
- Chỉ sử dụng thông tin có trong dữ liệu TravelAI khi nói về
  Tour và Khách sạn.
- Nếu có dữ liệu phù hợp, hãy giới thiệu rõ tên, điểm đến,
  giá, thời gian và đánh giá.
- Nếu dữ liệu có Link, hãy giữ nguyên Link đó trong câu trả lời.
- Nếu người dùng hỏi tiếp dựa trên câu trước, hãy sử dụng
  lịch sử hội thoại.
- Nếu không đủ dữ liệu, hãy nói rõ và đề nghị người dùng
  cung cấp thêm thông tin.
- Không tự tạo giá, tên Tour, tên Khách sạn hoặc đường dẫn.
- Không nói rằng bạn đã đặt tour/phòng nếu chưa có thao tác đặt
  thực tế trên website.
- Trả lời thân thiện, ngắn gọn và dễ hiểu.

THÔNG TIN NGƯỜI DÙNG VỪA HỎI:
{message}

ĐIỂM ĐẾN ĐƯỢC NHẬN DIỆN:
{destination?.Name ?? "Chưa nhận diện được"}

NGÂN SÁCH:
{(budget.HasValue ? $"{budget.Value:N0} VNĐ" : "Chưa xác định")}

SỐ NGƯỜI:
{(numberOfPeople.HasValue ? numberOfPeople.Value.ToString() : "Chưa xác định")}

SỐ NGÀY:
{(numberOfDays.HasValue ? numberOfDays.Value.ToString() : "Chưa xác định")}

========================
DỮ LIỆU TOUR TRONG DATABASE
========================

{tourData}

========================
DỮ LIỆU KHÁCH SẠN
========================

{hotelData}

========================
LỊCH SỬ HỘI THOẠI
========================

{historyText}

Hãy trả lời câu hỏi hiện tại dựa trên dữ liệu ở trên.
""";

            // =================================================
            // 14. GỌI OLLAMA
            // =================================================

            string aiResponse;

            try
            {
                aiResponse = await _ollamaService.AskAsync(prompt);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Lỗi AI: " + ex.Message,
                    conversationId = conversation.Id
                });
            }

            // =================================================
            // 15. LƯU CÂU TRẢ LỜI AI
            // =================================================

            var assistantMessage = new AIChatMessage
            {
                ConversationId = conversation.Id,
                Role = "Assistant",
                Content = aiResponse,
                CreatedAt = DateTime.Now
            };

            _context.AIChatMessages.Add(assistantMessage);

            conversation.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            // =================================================
            // 16. TRẢ KẾT QUẢ VỀ GIAO DIỆN
            // =================================================

            return Json(new
            {
                success = true,
                message = aiResponse,
                conversationId = conversation.Id
            });
        }
    }
}