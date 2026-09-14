using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelAI.Data;

namespace TravelAI.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminReviewController : Controller
    {
        private readonly TravelDbContext _context;

        public AdminReviewController(TravelDbContext context)
        {
            _context = context;
        }

        // ================= DANH SÁCH ĐÁNH GIÁ =================
        public async Task<IActionResult> Index()
        {
            var reviews = await _context.Reviews
                .Include(r => r.User)
                .Include(r => r.Tour)
                .Include(r => r.Hotel)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return View(reviews);
        }
    }
}