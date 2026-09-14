using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelAI.Data;

namespace TravelAI.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminPromotionController : Controller
    {
        private readonly TravelDbContext _context;

        public AdminPromotionController(TravelDbContext context)
        {
            _context = context;
        }

        // ================= DANH SÁCH KHUYẾN MÃI =================

        public async Task<IActionResult> Index()
        {
            var promotions = await _context.Promotions
                .OrderByDescending(p => p.Id)
                .ToListAsync();

            return View(promotions);
        }
    }
}