using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.RegularExpressions;
using TravelAI.Data;

namespace TravelAI.Controllers
{
    [Authorize]
    public class AIController : Controller
    {
        private readonly TravelDbContext _context;

        public AIController(TravelDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // TRANG TRAVEL AI
        // ==========================================

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        // ==========================================
        // CHAT VỚI TRAVEL AI
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Chat(string message)
        {
            // ==========================================
            // 1. KIỂM TRA NỘI DUNG
            // ==========================================

            if (string.IsNullOrWhiteSpace(message))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Vui lòng nhập nội dung cần tư vấn."
                });
            }

            // ==========================================
            // 2. LẤY USER ĐANG ĐĂNG NHẬP
            // ==========================================

            var userIdString = User.FindFirstValue(
                ClaimTypes.NameIdentifier
            );

            if (!int.TryParse(userIdString, out var userId))
            {
                return Unauthorized();
            }

            // ==========================================
            // 3. TẠO CUỘC HỘI THOẠI
            // ==========================================

            var conversation = new Models.AIConversation
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

            // ==========================================
            // 4. LƯU CÂU HỎI CỦA USER
            // ==========================================

            var userMessage = new Models.AIChatMessage
            {
                ConversationId = conversation.Id,
                Role = "User",
                Content = message,
                CreatedAt = DateTime.Now
            };

            _context.AIChatMessages.Add(userMessage);

            // ==========================================
            // 5. CHUẨN HÓA CÂU HỎI
            // ==========================================

            var searchText = message.Trim().ToLower();

            // ==========================================
            // 6. NHẬN DIỆN ĐIỂM ĐẾN
            // ==========================================

            var destinations = await _context.Destinations
                .AsNoTracking()
                .ToListAsync();

            var destination = destinations
                .FirstOrDefault(d =>
                    message.Contains(
                        d.Name,
                        StringComparison.OrdinalIgnoreCase
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

            // Nếu không nhận diện được điểm đến
            if (destination == null)
            {
                return Json(new
                {
                    success = true,
                    message =
                        "AI chưa nhận diện được điểm đến trong câu hỏi.\n\n" +
                        "Bạn có thể thử nhập tên điểm đến như " +
                        "Đà Nẵng, Đà Lạt, Phú Quốc, Nha Trang hoặc Sapa.",
                    conversationId = conversation.Id
                });
            }

            // ==========================================
            // 7. NHẬN DIỆN NGÂN SÁCH
            // ==========================================

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

            // ==========================================
            // 8. NHẬN DIỆN SỐ NGƯỜI
            // ==========================================

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
                    out var people
                )
            )
            {
                numberOfPeople = people;
            }

            // ==========================================
            // 9. NHẬN DIỆN SỐ NGÀY
            // ==========================================

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
                    out var days
                )
            )
            {
                numberOfDays = days;
            }

            // ==========================================
            // 10. TÌM TOUR
            // ==========================================

            var toursQuery = _context.Tours
                .Include(t => t.Destination)
                .Where(t => t.Status == "Active");

            // Lọc theo điểm đến
            if (destinationId.HasValue)
            {
                toursQuery = toursQuery
                    .Where(t =>
                        t.DestinationId == destinationId.Value
                    );
            }

            // Lọc theo ngân sách
            if (budget.HasValue)
            {
                toursQuery = toursQuery
                    .Where(t =>
                        (t.SalePrice ?? t.Price)
                        <= budget.Value
                    );
            }

            // Lọc theo số ngày
            if (numberOfDays.HasValue)
            {
                toursQuery = toursQuery
                    .Where(t =>
                        t.Duration == numberOfDays.Value
                    );
            }

            // Lấy Tour sau khi đã áp dụng TẤT CẢ bộ lọc
            var tours = await toursQuery
                .OrderByDescending(t => t.Rating)
                .Take(5)
                .ToListAsync();

            // ==========================================
            // 11. TÌM KHÁCH SẠN
            // ==========================================

            var hotelsQuery = _context.Hotels
                .Include(h => h.Destination)
                .Include(h => h.Rooms)
                .Where(h => h.Status == "Active");

            // Lọc khách sạn theo điểm đến
            if (destinationId.HasValue)
            {
                hotelsQuery = hotelsQuery
                    .Where(h =>
                        h.DestinationId == destinationId.Value
                    );
            }

            var hotels = await hotelsQuery
                .OrderByDescending(h => h.Rating)
                .Take(5)
                .ToListAsync();

            // ==========================================
            // 12. TẠO CÂU TRẢ LỜI
            // ==========================================

            string aiResponse = string.Empty;

            // ==========================================
            // 12.1. TOUR
            // ==========================================

            if (tours.Any())
            {
                var tourLines = tours.Select(t =>
                {
                    var price = t.SalePrice ?? t.Price;

                    var destinationName =
                        t.Destination?.Name
                        ?? "Chưa xác định";

                    var totalPrice =
                        numberOfPeople.HasValue
                        ? price * numberOfPeople.Value
                        : price;

                    var peopleInfo =
                        numberOfPeople.HasValue
                        ? $" - {numberOfPeople.Value} người: " +
                          $"{totalPrice:N0}đ"
                        : "";

                    return
                        $"• {t.Name} - " +
                        $"{destinationName} - " +
                        $"{price:N0}đ/người - " +
                        $"{t.Duration} ngày - " +
                        $"⭐ {t.Rating:0.0}/5" +
                        peopleInfo;
                });

                aiResponse +=
                    "🗺️ **Tour phù hợp:**\n\n" +
                    string.Join("\n", tourLines);
            }

            // ==========================================
            // 12.2. KHÁCH SẠN
            // ==========================================

            if (hotels.Any())
            {
                var hotelLines = hotels.Select(h =>
                {
                    var availableRoom = h.Rooms
                        .Where(r =>
                            r.Status == "Available"
                            &&
                            r.AvailableQuantity > 0
                        )
                        .OrderBy(r => r.PricePerNight)
                        .FirstOrDefault();

                    var roomInfo = availableRoom != null
                        ? $"{availableRoom.RoomName} - " +
                          $"{availableRoom.PricePerNight:N0}đ/đêm"
                        : "Chưa có phòng trống";

                    return
                        $"• {h.Name} - " +
                        $"{h.City ?? h.Province ?? "Chưa xác định"} - " +
                        $"⭐ {h.Rating:0.0}/5 - " +
                        $"{roomInfo}";
                });

                if (!string.IsNullOrEmpty(aiResponse))
                {
                    aiResponse += "\n\n";
                }

                aiResponse +=
                    "🏨 **Khách sạn phù hợp:**\n\n" +
                    string.Join("\n", hotelLines);
            }

            // ==========================================
            // 12.3. KHÔNG TÌM THẤY
            // ==========================================

            if (string.IsNullOrEmpty(aiResponse))
            {
                aiResponse =
                    "Tôi chưa tìm thấy tour hoặc khách sạn phù hợp " +
                    "với yêu cầu của bạn.\n\n" +

                    $"📍 Điểm đến: {destination.Name}\n";

                if (budget.HasValue)
                {
                    aiResponse +=
                        $"💰 Ngân sách: {budget.Value:N0}đ\n";
                }

                if (numberOfDays.HasValue)
                {
                    aiResponse +=
                        $"📅 Thời gian: {numberOfDays.Value} ngày\n";
                }

                if (numberOfPeople.HasValue)
                {
                    aiResponse +=
                        $"👥 Số người: {numberOfPeople.Value}\n";
                }

                aiResponse +=
                    "\nBạn có thể thử thay đổi ngân sách " +
                    "hoặc thời gian chuyến đi.";
            }

            // ==========================================
            // 13. LƯU CÂU TRẢ LỜI AI
            // ==========================================

            var assistantMessage = new Models.AIChatMessage
            {
                ConversationId = conversation.Id,
                Role = "Assistant",
                Content = aiResponse,
                CreatedAt = DateTime.Now
            };

            _context.AIChatMessages.Add(assistantMessage);

            conversation.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            // ==========================================
            // 14. TRẢ KẾT QUẢ VỀ GIAO DIỆN
            // ==========================================

            return Json(new
            {
                success = true,
                message = aiResponse,
                conversationId = conversation.Id
            });
        }
    }
}