using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelAI.Data;

namespace TravelAI.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminUserController : Controller
    {
        private readonly TravelDbContext _context;

        public AdminUserController(TravelDbContext context)
        {
            _context = context;
        }

        // =========================
        // DANH SÁCH USER
        // =========================

        public async Task<IActionResult> Index(
            string? searchString,
            string? role,
            string? status)
        {
            var users = _context.Users
                .AsQueryable();

            // Tìm kiếm
            if (!string.IsNullOrWhiteSpace(searchString))
            {
                searchString = searchString.Trim();

                users = users.Where(u =>
                    u.FullName.Contains(searchString) ||
                    u.Email.Contains(searchString));
            }

            // Lọc Role
            if (!string.IsNullOrWhiteSpace(role) &&
                role != "All")
            {
                users = users.Where(u => u.Role == role);
            }

            // Lọc Status
            if (!string.IsNullOrWhiteSpace(status) &&
                status != "All")
            {
                users = users.Where(u => u.Status == status);
            }

            var result = await users
                .OrderByDescending(u => u.Id)
                .ToListAsync();

            ViewBag.SearchString = searchString;
            ViewBag.Role = role ?? "All";
            ViewBag.Status = status ?? "All";

            return View(result);
        }

        // =========================
        // CHI TIẾT USER
        // =========================

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // =========================
        // ĐỔI ROLE
        // =========================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeRole(
            int id,
            string role)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            var allowedRoles = new[]
            {
                "User",
                "Admin"
            };

            if (!allowedRoles.Contains(role))
            {
                TempData["Error"] =
                    "Quyền người dùng không hợp lệ.";

                return RedirectToAction(nameof(Index));
            }

            // Không cho Admin tự hạ quyền chính mình
            var currentUserId =
                User.FindFirst(
                    System.Security.Claims.ClaimTypes.NameIdentifier
                )?.Value;

            if (int.TryParse(currentUserId, out var currentId) &&
                currentId == user.Id &&
                user.Role == "Admin" &&
                role != "Admin")
            {
                TempData["Error"] =
                    "Bạn không thể tự hạ quyền Admin của chính mình.";

                return RedirectToAction(nameof(Index));
            }

            user.Role = role;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                role == "Admin"
                    ? "Đã cấp quyền Admin cho người dùng."
                    : "Đã chuyển người dùng về quyền User.";

            return RedirectToAction(nameof(Index));
        }

        // =========================
        // KHÓA / MỞ KHÓA USER
        // =========================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeStatus(int id)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            // Không cho Admin tự khóa chính mình
            var currentUserId =
                User.FindFirst(
                    System.Security.Claims.ClaimTypes.NameIdentifier
                )?.Value;

            if (int.TryParse(currentUserId, out var currentId) &&
                currentId == user.Id)
            {
                TempData["Error"] =
                    "Bạn không thể tự khóa tài khoản của chính mình.";

                return RedirectToAction(nameof(Index));
            }

            if (user.Status == "Active")
            {
                user.Status = "Locked";

                TempData["Success"] =
                    "Đã khóa tài khoản người dùng.";
            }
            else
            {
                user.Status = "Active";

                TempData["Success"] =
                    "Đã mở khóa tài khoản người dùng.";
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}