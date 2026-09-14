using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelAI.Data;

namespace TravelAI.Controllers
{
    public class ComboController : Controller
    {
        private readonly TravelDbContext _context;

        public ComboController(TravelDbContext context)
        {
            _context = context;
        }

        // ================= DANH SÁCH COMBO =================

        public async Task<IActionResult> Index()
        {
            var combos = await _context.Combos
                .Where(c => c.Status == "Active")
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            return View(combos);
        }

        // ================= CHI TIẾT COMBO =================

        public async Task<IActionResult> Details(int id)
        {
            var combo = await _context.Combos
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c =>
                    c.Id == id &&
                    c.Status == "Active");

            if (combo == null)
            {
                return NotFound();
            }

            return View(combo);
        }
    }
}