using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelAI.Data;

namespace TravelAI.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AIConversationAdminController : Controller
    {
        private readonly TravelDbContext _context;

        public AIConversationAdminController(
            TravelDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Chỉ lấy hội thoại của tài khoản người dùng.
            // Không lấy hội thoại của tài khoản Admin.
            var conversations =
                await _context.AIConversations
                    .Include(x => x.User)
                    .Include(x => x.Messages)
                    .Where(x =>
                        x.User != null &&
                        x.User.Role != "Admin")
                    .OrderByDescending(x => x.UpdatedAt)
                    .ToListAsync();

            return View(conversations);
        }
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var conversation =
                await _context.AIConversations
                    .Include(x => x.User)
                    .Include(x => x.Messages)
                    .FirstOrDefaultAsync(x =>
                        x.Id == id &&
                        x.User != null &&
                        x.User.Role != "Admin");

            if (conversation == null)
            {
                return NotFound();
            }

            return View(conversation);
        }
    }
}