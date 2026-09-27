using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TravelAI.Data;
using TravelAI.Models;
using TravelAI.Services;

namespace TravelAI.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminAIController : Controller
    {
        private readonly TravelDbContext _context;
        private readonly AdminAIAgentService _agentService;

        public AdminAIController(
            TravelDbContext context,
            AdminAIAgentService agentService)
        {
            _context = context;
            _agentService = agentService;
        }

        // =========================================================
        // 1. TRANG ADMIN AI
        // =========================================================

        public async Task<IActionResult> Index()
        {
            var userIdClaim =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdClaim, out int userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var conversations = await _context.AIConversations
                .Where(x =>
                    x.UserId == userId &&
                    x.Messages.Any())
                .Include(x => x.User)
                .Include(x => x.Messages)
                .OrderByDescending(x => x.UpdatedAt)
                .ToListAsync();

            return View(conversations);
        }

        // =========================================================
        // 2. GỬI CÂU HỎI CHO ADMIN AI
        // =========================================================

        [HttpPost]
        public async Task<IActionResult> Chat(
            string message,
            int? conversationId)
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
                // -------------------------------------------------
                // Lấy ID Admin đang đăng nhập
                // -------------------------------------------------

                var userIdClaim =
                    User.FindFirstValue(
                        ClaimTypes.NameIdentifier);

                if (!int.TryParse(
                    userIdClaim,
                    out int userId))
                {
                    return Json(new
                    {
                        success = false,
                        message =
                            "Không xác định được tài khoản quản trị."
                    });
                }

                // -------------------------------------------------
                // Tìm đúng cuộc hội thoại
                // -------------------------------------------------

                AIConversation? conversation = null;

                if (conversationId.HasValue)
                {
                    conversation =
                        await _context.AIConversations
                            .Include(x => x.Messages)
                            .FirstOrDefaultAsync(x =>
                                x.Id == conversationId.Value &&
                                x.UserId == userId);
                }

                // -------------------------------------------------
                // Nếu không có conversationId
                // thì tạo cuộc hội thoại mới.
                //
                // KHÔNG lấy cuộc trò chuyện gần nhất.
                // -------------------------------------------------

                if (conversation == null)
                {
                    conversation =
                        new AIConversation
                        {
                            UserId = userId,

                            Title =
                                CreateConversationTitle(
                                    message),

                            CreatedAt =
                                DateTime.Now,

                            UpdatedAt =
                                DateTime.Now
                        };

                    _context.AIConversations.Add(
                        conversation);

                    await _context.SaveChangesAsync();

                    // Sau SaveChangesAsync()
                    // conversation.Id đã được tạo.
                    conversation.Messages =
                        new List<AIChatMessage>();
                }

                // -------------------------------------------------
                // Lấy lịch sử hội thoại TRƯỚC câu hỏi hiện tại
                // -------------------------------------------------

                var conversationHistory =
                    await _context.AIChatMessages
                        .AsNoTracking()
                        .Where(x =>
                            x.ConversationId ==
                            conversation.Id)
                        .OrderBy(x => x.CreatedAt)
                        .ToListAsync();

                // -------------------------------------------------
                // Lưu câu hỏi của Admin
                // -------------------------------------------------

                var userMessage =
                    new AIChatMessage
                    {
                        ConversationId =
                            conversation.Id,

                        Role = "User",

                        Content =
                            message.Trim(),

                        CreatedAt =
                            DateTime.Now
                    };

                _context.AIChatMessages.Add(
                    userMessage);

                // -------------------------------------------------
                // Gọi Admin AI Agent
                //
                // TRUYỀN LỊCH SỬ VÀO AI
                // để AI hiểu câu hỏi nối tiếp.
                // -------------------------------------------------

                string aiResponse =
                    await _agentService.AskAsync(
                        message.Trim(),
                        conversationHistory);

                // -------------------------------------------------
                // Lưu câu trả lời của AI
                // -------------------------------------------------

                var assistantMessage =
                    new AIChatMessage
                    {
                        ConversationId =
                            conversation.Id,

                        Role = "Assistant",

                        Content =
                            aiResponse,

                        CreatedAt =
                            DateTime.Now
                    };

                _context.AIChatMessages.Add(
                    assistantMessage);

                // -------------------------------------------------
                // Cập nhật thời gian hội thoại
                // -------------------------------------------------

                conversation.UpdatedAt =
                    DateTime.Now;

                // -------------------------------------------------
                // Tiêu đề chỉ lấy từ câu hỏi đầu tiên
                // -------------------------------------------------

                var userMessageCount =
                    await _context.AIChatMessages
                        .CountAsync(x =>
                            x.ConversationId ==
                            conversation.Id &&
                            x.Role == "User");

                if (userMessageCount == 0)
                {
                    conversation.Title =
                        CreateConversationTitle(
                            message);
                }

                // -------------------------------------------------
                // Lưu tất cả
                // -------------------------------------------------

                await _context.SaveChangesAsync();

                // -------------------------------------------------
                // Trả kết quả về JavaScript
                // -------------------------------------------------

                return Json(new
                {
                    success = true,

                    message = aiResponse,

                    conversationId =
                        conversation.Id,

                    title =
                        conversation.Title,

                    createdAt =
                        DateTime.Now
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,

                    message =
                        "Lỗi AI: " +
                        ex.Message
                });
            }
        }

        // =========================================================
        // 3. LẤY LỊCH SỬ TIN NHẮN
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> GetMessages(
            int conversationId)
        {
            try
            {
                var userIdClaim =
                    User.FindFirstValue(
                        ClaimTypes.NameIdentifier);

                if (!int.TryParse(
                    userIdClaim,
                    out int userId))
                {
                    return Json(new
                    {
                        success = false,
                        message =
                            "Không xác định được tài khoản."
                    });
                }

                // -------------------------------------------------
                // Kiểm tra conversation có thuộc Admin này không
                // -------------------------------------------------

                var conversation =
                    await _context.AIConversations
                        .AsNoTracking()
                        .FirstOrDefaultAsync(x =>
                            x.Id == conversationId &&
                            x.UserId == userId);

                if (conversation == null)
                {
                    return Json(new
                    {
                        success = false,
                        message =
                            "Không tìm thấy cuộc trò chuyện."
                    });
                }

                // -------------------------------------------------
                // Lấy toàn bộ tin nhắn
                // -------------------------------------------------

                var messages =
                    await _context.AIChatMessages
                        .AsNoTracking()
                        .Where(x =>
                            x.ConversationId ==
                            conversationId)
                        .OrderBy(x => x.CreatedAt)
                        .Select(x => new
                        {
                            role = x.Role,

                            content = x.Content,

                            createdAt =
                                x.CreatedAt
                        })
                        .ToListAsync();

                return Json(new
                {
                    success = true,

                    conversationId =
                        conversation.Id,

                    title =
                        conversation.Title,

                    messages =
                        messages
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,

                    message =
                        "Không thể tải lịch sử: " +
                        ex.Message
                });
            }
        }

        // =========================================================
        // 4. TẠO CUỘC TRÒ CHUYỆN MỚI
        // =========================================================
        //
        // QUAN TRỌNG:
        // Không tạo bản ghi trong Database ở đây.
        //
        // Khi người dùng bấm "Đoạn chat mới":
        // - JavaScript chỉ reset giao diện
        // - currentConversationId = null
        //
        // Khi người dùng gửi câu hỏi đầu tiên:
        // - Chat() mới tạo AIConversation
        // - Tiêu đề lấy từ câu hỏi đầu tiên
        //
        // Nhờ vậy sẽ không còn:
        // "Cuộc trò chuyện mới - 0 tin nhắn"
        //

        [HttpPost]
        public IActionResult NewConversation()
        {
            return Json(new
            {
                success = true,

                conversationId =
                    (int?)null,

                title =
                    "Cuộc trò chuyện mới"
            });
        }

        // =========================================================
        // 5. TẠO TIÊU ĐỀ TỪ CÂU HỎI ĐẦU TIÊN
        // =========================================================

        private string CreateConversationTitle(
            string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return "Admin AI";
            }

            var title =
                message
                    .Trim()
                    .Replace("\r", " ")
                    .Replace("\n", " ");

            // -------------------------------------------------
            // Xóa khoảng trắng thừa
            // -------------------------------------------------

            while (title.Contains("  "))
            {
                title =
                    title.Replace(
                        "  ",
                        " ");
            }

            // -------------------------------------------------
            // Giới hạn tiêu đề
            // -------------------------------------------------

            if (title.Length > 60)
            {
                title =
                    title.Substring(0, 60)
                    .TrimEnd()
                    + "...";
            }

            return title;
        }
    }
}