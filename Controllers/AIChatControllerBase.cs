using System.Diagnostics;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TravelAI.Data;
using TravelAI.Models;
using TravelAI.Services;
using TravelAI.ViewModels;

namespace TravelAI.Controllers;

public abstract class AIChatControllerBase(TravelDbContext db, OllamaService ollama,
    TravelAIService travel, AdminAIAgentService admin, AIRequestGate gate,
    IOptions<AIOptions> options, ILoggerFactory logFactory) : Controller
{
    protected abstract string ChatScope { get; }
    private bool IsAdmin => ChatScope == "Admin";
    private int UserId => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
    private IQueryable<AIConversation> Owned => db.AIConversations.Where(x => x.UserId == UserId && x.Scope == ChatScope);

    [HttpGet]
    public async Task<IActionResult> Index(int? conversationId, CancellationToken ct)
    {
        var current = conversationId.HasValue ? await Owned.Include(x => x.Messages).FirstOrDefaultAsync(x => x.Id == conversationId, ct) : null;
        if (conversationId.HasValue && current == null) return NotFound();
        if (current != null) current.Messages = current.Messages.OrderBy(x => x.Id).ToList();
        return View("Index", new AIChatViewModel { CurrentConversation = current,
            Conversations = await Owned.AsNoTracking().OrderByDescending(x => x.UpdatedAt).ToListAsync(ct),
            IsAdmin = IsAdmin, ModelName = options.Value.Model });
    }

    [HttpGet]
    public async Task<IActionResult> Conversations(CancellationToken ct) => Json(new { success = true,
        conversations = await Owned.AsNoTracking().OrderByDescending(x => x.UpdatedAt)
            .Select(x => new { x.Id, x.Title, x.UpdatedAt }).ToListAsync(ct) });

    [HttpGet]
    public async Task<IActionResult> GetMessages(int conversationId, CancellationToken ct)
    {
        var conversation = await Owned.AsNoTracking().FirstOrDefaultAsync(x => x.Id == conversationId, ct);
        if (conversation == null) return NotFound(new { success = false, message = "Không tìm thấy cuộc trò chuyện." });
        return Json(new { success = true, conversationId, conversation.Title,
            messages = await db.AIChatMessages.AsNoTracking().Where(x => x.ConversationId == conversationId).OrderBy(x => x.Id)
                .Select(x => new { x.Id, x.Role, x.Content, x.CreatedAt }).ToListAsync(ct) });
    }

    [HttpGet]
    public async Task<IActionResult> Status(CancellationToken ct) => Json(await ollama.GetStatusAsync(ct));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Rename(int conversationId, string title, CancellationToken ct)
    {
        if (!gate.TryEnterUser(UserId)) return Conflict(new { message = "Hãy dừng hoặc chờ lượt trả lời hiện tại." });
        try
        {
            if (string.IsNullOrWhiteSpace(title) || title.Length > 200) return BadRequest(new { message = "Tiêu đề cần từ 1 đến 200 ký tự." });
            var conversation = await Owned.FirstOrDefaultAsync(x => x.Id == conversationId, ct);
            if (conversation == null) return NotFound();
            conversation.Title = title.Trim(); await db.SaveChangesAsync(ct);
            return Json(new { success = true });
        }
        finally { gate.ExitUser(UserId); }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int conversationId, CancellationToken ct)
    {
        if (!gate.TryEnterUser(UserId)) return Conflict(new { message = "Hãy dừng hoặc chờ lượt trả lời hiện tại." });
        try
        {
            var conversation = await Owned.Include(x => x.Messages).FirstOrDefaultAsync(x => x.Id == conversationId, ct);
            if (conversation == null) return NotFound();
            db.AIChatMessages.RemoveRange(conversation.Messages); db.AIConversations.Remove(conversation);
            await db.SaveChangesAsync(ct); return Json(new { success = true });
        }
        finally { gate.ExitUser(UserId); }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Chat([FromForm] AIChatRequest request)
    {
        var requestAborted = HttpContext.RequestAborted;
        var logger = logFactory.CreateLogger(GetType());
        if (!ModelState.IsValid || string.IsNullOrWhiteSpace(request.Message))
            return BadRequest(new { success = false, message = "Nhập câu hỏi từ 1 đến 2.000 ký tự." });
        if (UserId == 0) return Unauthorized(new { success = false, code = "invalid_session",
            message = "Phiên đăng nhập không hợp lệ. Hãy đăng xuất rồi đăng nhập lại." });
        if (!gate.TryEnterUser(UserId)) return Conflict(new { success = false, message = "Một lượt chat đang chạy. Hãy dừng hoặc chờ hoàn tất." });
        var watch = Stopwatch.StartNew();
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(requestAborted);
        // Reserve up to two seconds for saving the final/partial answer.
        deadline.CancelAfter(TimeSpan.FromSeconds(options.Value.ResponseTimeoutSeconds - 2));
        var content = new StringBuilder();
        AIConversation? conversation = null;
        AIChatMessage? latestUser = null;
        var mode = "data";
        var streaming = false;
        var modelEntered = false;
        var history = new List<AIHistoryMessage>();
        try
        {
            var account = await db.Users.AsNoTracking().Where(x => x.Id == UserId)
                .Select(x => new { x.Status, x.Role }).FirstOrDefaultAsync(deadline.Token);
            if (account == null) return Unauthorized(new { success = false, code = "invalid_session",
                message = "Tài khoản của phiên đăng nhập không còn tồn tại. Hãy đăng xuất rồi đăng nhập lại." });
            var status = account.Status?.Trim();
            if (string.Equals(status, "Locked", StringComparison.OrdinalIgnoreCase))
                return StatusCode(StatusCodes.Status403Forbidden, new { success = false, code = "account_locked",
                    message = "Tài khoản đã bị khóa. Hãy liên hệ quản trị viên để được hỗ trợ." });
            // AddUserStatus used defaultValue: "" for accounts created before that migration.
            // These accounts can log in under the existing AccountController policy.
            // Accept only that legacy empty value or Active; never rewrite/unlock accounts here.
            if (!string.IsNullOrEmpty(status) && !string.Equals(status, "Active", StringComparison.OrdinalIgnoreCase))
                return StatusCode(StatusCodes.Status403Forbidden, new { success = false, code = "account_inactive",
                    message = "Tài khoản chưa ở trạng thái hoạt động. Hãy nhờ quản trị viên kiểm tra trong trang quản lí người dùng." });
            if (IsAdmin && account.Role != "Admin")
                return StatusCode(StatusCodes.Status403Forbidden, new { success = false, code = "admin_required",
                    message = "Quyền quản trị của tài khoản đã thay đổi. Hãy đăng nhập lại bằng tài khoản Admin để xem báo cáo." });
            if (request.ConversationId.HasValue)
            {
                conversation = await Owned.FirstOrDefaultAsync(x => x.Id == request.ConversationId, deadline.Token);
                if (conversation == null) return NotFound(new { success = false, message = "Không tìm thấy cuộc trò chuyện." });
            }
            else if (request.Regenerate) return BadRequest(new { message = "Chưa có câu trả lời để tạo lại." });
            if (conversation == null)
            {
                conversation = new AIConversation { UserId = UserId, Scope = ChatScope,
                    Title = AIText.Short(request.Message.Trim().Replace("\n", " "), 70) };
                db.AIConversations.Add(conversation);
            }
            if (conversation.Id != 0)
            {
                if (request.Regenerate)
                {
                    latestUser = await db.AIChatMessages.Where(x => x.ConversationId == conversation.Id && x.Role == "User")
                        .OrderByDescending(x => x.Id).FirstOrDefaultAsync(deadline.Token);
                    if (latestUser == null) return BadRequest(new { message = "Chưa có câu hỏi để tạo lại." });
                    request.Message = latestUser.Content;
                }
                var beforeId = latestUser?.Id ?? int.MaxValue;
                var recent = await db.AIChatMessages.AsNoTracking().Where(x => x.ConversationId == conversation.Id && x.Id < beforeId)
                    .OrderByDescending(x => x.Id).Take(options.Value.HistoryMessages).ToListAsync(deadline.Token);
                history = recent.OrderBy(x => x.Id).Select(x => new AIHistoryMessage(x.Role, x.Content)).ToList();
            }
            if (!request.Regenerate)
            {
                latestUser = new AIChatMessage { Conversation = conversation, Role = "User", Content = request.Message.Trim() };
                db.AIChatMessages.Add(latestUser);
                conversation.UpdatedAt = DateTime.Now;
                await db.SaveChangesAsync(deadline.Token);
            }
            Response.ContentType = "text/event-stream; charset=utf-8";
            Response.Headers.CacheControl = "no-cache, no-store";
            Response.Headers["X-Accel-Buffering"] = "no";
            streaming = true;
            await Emit("meta", new { conversationId = conversation.Id, title = conversation.Title, model = options.Value.Model }, requestAborted);
            var prepared = IsAdmin ? await admin.PrepareAsync(request.Message, history, deadline.Token)
                : await travel.PrepareAsync(request.Message, history, deadline.Token);
            content.Append(prepared.GroundedAnswer);
            await Emit("delta", new { text = prepared.GroundedAnswer }, requestAborted);
            await Emit("sources", new { tours = prepared.Tours, actions = prepared.Actions }, requestAborted);
            if (gate.TryEnterModel())
            {
                modelEntered = true;
                using var generation = CancellationTokenSource.CreateLinkedTokenSource(deadline.Token);
                generation.CancelAfter(TimeSpan.FromSeconds(options.Value.GenerationTimeoutSeconds));
                try
                {
                    var started = false;
                    await foreach (var chunk in ollama.StreamAsync(prepared.SystemPrompt, history, request.Message, prepared.Evidence, generation.Token))
                    {
                        if (!started)
                        {
                            const string heading = "\n\n**Diễn giải AI**\n\n";
                            content.Append(heading); await Emit("delta", new { text = heading }, requestAborted); started = true;
                        }
                        content.Append(chunk); await Emit("delta", new { text = chunk }, requestAborted);
                    }
                    mode = started ? "ollama" : "data";
                }
                catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or JsonException)
                {
                    if (requestAborted.IsCancellationRequested) throw;
                    logger.LogInformation(ex, "AI dùng kết quả dữ liệu, conversation {ConversationId}", conversation.Id);
                    var note = "\n\n_Phần diễn giải AI chưa hoàn tất trong thời gian cho phép hoặc Ollama chưa sẵn sàng. Thông tin và link ở trên được lấy trực tiếp từ hệ thống._";
                    content.Append(note); await Emit("delta", new { text = note }, requestAborted); mode = "fallback";
                }
            }
            else
            {
                const string note = "\n\n_Model đang bận/nạp. Kết quả ở trên là dữ liệu trực tiếp từ hệ thống; bạn có thể tạo lại phần tư vấn sau._";
                content.Append(note); await Emit("delta", new { text = note }, requestAborted); mode = "busy";
            }
        }
        catch (OperationCanceledException) when (requestAborted.IsCancellationRequested)
        { content.Append("\n\n_Đã dừng tạo câu trả lời._"); mode = "stopped"; }
        catch (Exception ex)
        {
            logger.LogError(ex, "Không hoàn tất lượt chat {Scope}", ChatScope);
            if (!streaming) return StatusCode(503, new { success = false, message = "Chưa lấy được dữ liệu. Kiểm tra kết nối database rồi thử lại." });
            var note = ex is OperationCanceledException ? "\n\n_Đã hết thời gian lấy dữ liệu. Hãy thu hẹp câu hỏi hoặc kiểm tra database._" :
                "\n\n_Chưa hoàn tất tra cứu dữ liệu. Hãy thử lại._";
            content.Append(note); mode = "error";
            if (!requestAborted.IsCancellationRequested) await Emit("delta", new { text = note }, requestAborted);
        }
        finally
        {
            if (modelEntered) gate.ExitModel();
            try
            {
            if (conversation?.Id > 0 && content.Length > 0)
            {
                try
                {
                    // A short independent token lets Stop preserve partial output after the browser disconnects.
                    using var persistence = new CancellationTokenSource(TimeSpan.FromSeconds(Math.Clamp(options.Value.ResponseTimeoutSeconds - watch.Elapsed.TotalSeconds, 0.1, 2)));
                    if (request.Regenerate && latestUser != null)
                    {
                        var oldAnswers = await db.AIChatMessages.Where(x => x.ConversationId == conversation.Id && x.Id > latestUser.Id && x.Role == "Assistant").ToListAsync(persistence.Token);
                        db.AIChatMessages.RemoveRange(oldAnswers);
                    }
                    db.AIChatMessages.Add(new AIChatMessage { ConversationId = conversation.Id, Role = "Assistant", Content = content.ToString() });
                    conversation.UpdatedAt = DateTime.Now; await db.SaveChangesAsync(persistence.Token);
                    if (!requestAborted.IsCancellationRequested)
                        await Emit("done", new { conversationId = conversation.Id, mode, elapsedMs = watch.ElapsedMilliseconds, saved = true }, requestAborted);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Không lưu được câu trả lời AI");
                    if (!requestAborted.IsCancellationRequested && streaming)
                    {
                        try { await Emit("done", new { mode = "save-error", elapsedMs = watch.ElapsedMilliseconds, saved = false }, requestAborted); }
                        catch (OperationCanceledException) { }
                        catch (IOException) { }
                    }
                }
            }
            }
            finally { gate.ExitUser(UserId); }
        }
        return new EmptyResult();
    }

    private async Task Emit(string eventName, object value, CancellationToken ct)
    {
        await Response.WriteAsync($"event: {eventName}\ndata: {JsonSerializer.Serialize(value, new JsonSerializerOptions(JsonSerializerDefaults.Web))}\n\n", ct);
        await Response.Body.FlushAsync(ct);
    }
}
