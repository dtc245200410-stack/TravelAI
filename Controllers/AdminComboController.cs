using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelAI.Data;

namespace TravelAI.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminComboController : Controller
    {
        private readonly TravelDbContext _context;

        public AdminComboController(TravelDbContext context)
        {
            _context = context;
        }

        // ================= DANH SÁCH COMBO =================

        public async Task<IActionResult> Index()
        {
            var combos = await _context.Combos
                .OrderByDescending(c => c.Id)
                .ToListAsync();

            return View(combos);
        }
    }
}