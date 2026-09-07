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

        // ================= DANH SÁCH USER =================

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
            if (!string.IsNullOrWhiteSpace(role))
            {
                users = users.Where(u => u.Role == role);
            }

            // Lọc Status
            if (!string.IsNullOrWhiteSpace(status))
            {
                users = users.Where(u => u.Status == status);
            }

            var userList = await users
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();

            ViewBag.SearchString = searchString;
            ViewBag.Role = role;
            ViewBag.Status = status;

            // Thống kê
            ViewBag.TotalUsers = await _context.Users.CountAsync();

            ViewBag.TotalAdmins = await _context.Users
                .CountAsync(u => u.Role == "Admin");

            ViewBag.TotalCustomers = await _context.Users
                .CountAsync(u => u.Role == "Customer");

            ViewBag.ActiveUsers = await _context.Users
                .CountAsync(u => u.Status == "Active");

            ViewBag.LockedUsers = await _context.Users
                .CountAsync(u => u.Status == "Locked");

            return View(userList);
        }

        // ================= CHI TIẾT USER =================

        public async Task<IActionResult> Details(int id)
        {
            var user = await _context.Users
                .Include(u => u.Bookings)
                .Include(u => u.HotelBookings)
                .Include(u => u.Reviews)
                .Include(u => u.Favorites)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // ================= ĐỔI QUYỀN =================

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

            // Lấy ID Admin hiện tại
            var currentUserId = User.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier);

            if (currentUserId != null &&
                int.TryParse(currentUserId.Value, out int currentId))
            {
                // Không cho Admin tự đổi quyền của chính mình
                if (user.Id == currentId)
                {
                    TempData["Error"] =
                        "Bạn không thể tự thay đổi quyền của chính mình.";

                    return RedirectToAction(nameof(Index));
                }
            }

            // Chỉ cho phép 2 Role hợp lệ
            if (role != "Admin" &&
                role != "Customer")
            {
                TempData["Error"] =
                    "Quyền người dùng không hợp lệ.";

                return RedirectToAction(nameof(Index));
            }

            user.Role = role;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"Đã thay đổi quyền của {user.FullName} thành {role}.";

            return RedirectToAction(nameof(Index));
        }

        // ================= KHÓA / MỞ KHÓA =================

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

            // Lấy ID Admin hiện tại
            var currentUserId = User.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier);

            if (currentUserId != null &&
                int.TryParse(currentUserId.Value, out int currentId))
            {
                // Không cho Admin tự khóa mình
                if (user.Id == currentId)
                {
                    TempData["Error"] =
                        "Bạn không thể tự khóa tài khoản của mình.";

                    return RedirectToAction(nameof(Index));
                }
            }

            // Đổi trạng thái
            if (user.Status == "Active")
            {
                user.Status = "Locked";

                TempData["Success"] =
                    $"Đã khóa tài khoản {user.FullName}.";
            }
            else
            {
                user.Status = "Active";

                TempData["Success"] =
                    $"Đã mở khóa tài khoản {user.FullName}.";
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}