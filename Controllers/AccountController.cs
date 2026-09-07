using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TravelAI.Data;
using TravelAI.Models;

namespace TravelAI.Controllers
{
    public class AccountController : Controller
    {
        private readonly TravelDbContext _context;
        private readonly PasswordHasher<User> _passwordHasher;

        public AccountController(TravelDbContext context)
        {
            _context = context;
            _passwordHasher = new PasswordHasher<User>();
        }

        // ================= ĐĂNG NHẬP =================

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(
            string email,
            string password,
            bool rememberMe = false)
        {
            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Vui lòng nhập đầy đủ email và mật khẩu.";
                return View();
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email.Trim());

            if (user == null)
            {
                ViewBag.Error = "Email hoặc mật khẩu không chính xác.";
                return View();
            }

            // ================= KIỂM TRA TÀI KHOẢN =================

            if (user.Status == "Locked")
            {
                ViewBag.Error =
                    "Tài khoản của bạn đã bị khóa. Vui lòng liên hệ quản trị viên.";

                return View();
            }

            // ================= KIỂM TRA MẬT KHẨU =================

            var result = _passwordHasher.VerifyHashedPassword(
                user,
                user.PasswordHash,
                password);

            if (result == PasswordVerificationResult.Failed)
            {
                ViewBag.Error = "Email hoặc mật khẩu không chính xác.";
                return View();
            }

            // ================= TẠO CLAIM =================

            var claims = new List<Claim>
            {
                new Claim(
                    ClaimTypes.NameIdentifier,
                    user.Id.ToString()),

                new Claim(
                    ClaimTypes.Name,
                    user.FullName),

                new Claim(
                    ClaimTypes.Email,
                    user.Email),

                new Claim(
                    ClaimTypes.Role,
                    user.Role)
            };

            var claimsIdentity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme);

            // ================= THỜI GIAN ĐĂNG NHẬP =================

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = rememberMe,

                ExpiresUtc = rememberMe
                    ? DateTimeOffset.UtcNow.AddDays(7)
                    : DateTimeOffset.UtcNow.AddHours(2)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            // ================= ĐĂNG NHẬP THÀNH CÔNG =================

            return RedirectToAction("Index", "Home");
        }

        // ================= ĐĂNG KÝ =================

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(
            string fullName,
            string email,
            string password,
            string confirmPassword)
        {
            // Kiểm tra dữ liệu bắt buộc
            if (string.IsNullOrWhiteSpace(fullName) ||
                string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(confirmPassword))
            {
                ViewBag.Error = "Vui lòng nhập đầy đủ thông tin.";
                return View();
            }

            // Kiểm tra mật khẩu
            if (password.Length < 6)
            {
                ViewBag.Error =
                    "Mật khẩu phải có ít nhất 6 ký tự.";

                return View();
            }

            // Kiểm tra xác nhận mật khẩu
            if (password != confirmPassword)
            {
                ViewBag.Error =
                    "Mật khẩu xác nhận không khớp.";

                return View();
            }

            // Kiểm tra email đã tồn tại
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(
                    u => u.Email == email.Trim());

            if (existingUser != null)
            {
                ViewBag.Error =
                    "Email này đã được đăng ký.";

                return View();
            }

            // ================= TẠO USER MỚI =================

            var user = new User
            {
                FullName = fullName.Trim(),
                Email = email.Trim(),
                Role = "Customer",
                Status = "Active",
                CreatedAt = DateTime.Now
            };

            // Hash mật khẩu
            user.PasswordHash =
                _passwordHasher.HashPassword(
                    user,
                    password);

            // Lưu database
            _context.Users.Add(user);

            await _context.SaveChangesAsync();

            // Đăng ký thành công
            TempData["Success"] =
                "Đăng ký tài khoản thành công. Vui lòng đăng nhập.";

            return RedirectToAction("Login");
        }

        // ================= ĐĂNG XUẤT =================

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);

            return RedirectToAction(
                "Index",
                "Home");
        }

        // ================= ACCESS DENIED =================

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}