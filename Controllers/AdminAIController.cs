using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelAI.Data;

namespace TravelAI.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminAIController : Controller
    {
        private readonly TravelDbContext _context;

        public AdminAIController(TravelDbContext context)
        {
            _context = context;
        }

        // ================= DANH SÁCH HỘI THOẠI AI =================
        public async Task<IActionResult> Index()
        {
            var conversations = await _context.AIConversations
                .Include(c => c.User)
                .Include(c => c.Messages)
                .OrderByDescending(c => c.UpdatedAt)
                .ToListAsync();

            return View(conversations);
        }
    }
}