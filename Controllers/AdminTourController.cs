using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelAI.Data;
using TravelAI.Models;

namespace TravelAI.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminTourController : Controller
    {
        private readonly TravelDbContext _context;

        public AdminTourController(TravelDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // DANH SÁCH TOUR
        // =========================================================

        public async Task<IActionResult> Index(
            string? searchString,
            string? status,
            int page = 1)
        {
            const int pageSize = 10;

            if (page < 1)
            {
                page = 1;
            }

            var tours = _context.Tours
                .Include(t => t.Destination)
                .AsQueryable();

            // =========================
            // TÌM KIẾM
            // =========================

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                searchString = searchString.Trim();

                tours = tours.Where(t =>
                    t.Name.Contains(searchString) ||
                    (t.DepartureLocation != null &&
                     t.DepartureLocation.Contains(searchString)) ||
                    (t.Destination != null &&
                     t.Destination.Name.Contains(searchString)));
            }

            // =========================
            // LỌC TRẠNG THÁI
            // =========================

            if (!string.IsNullOrWhiteSpace(status))
            {
                tours = tours.Where(t => t.Status == status);
            }

            // =========================
            // SẮP XẾP
            // =========================

            tours = tours
                .OrderByDescending(t => t.CreatedAt)
                .ThenByDescending(t => t.Id);

            // =========================
            // PHÂN TRANG
            // =========================

            var totalTours = await tours.CountAsync();

            var totalPages = (int)Math.Ceiling(
                totalTours / (double)pageSize);

            if (totalPages > 0 && page > totalPages)
            {
                page = totalPages;
            }

            var result = await tours
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // =========================
            // VIEW DATA
            // =========================

            ViewBag.SearchString = searchString;
            ViewBag.SelectedStatus = status;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalTours = totalTours;

            return View(result);
        }

        // =========================================================
        // CHI TIẾT TOUR
        // =========================================================

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var tour = await _context.Tours
                .Include(t => t.Destination)
                .Include(t => t.Images)
                .Include(t => t.Bookings)
                .Include(t => t.Reviews)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tour == null)
            {
                return NotFound();
            }

            return View(tour);
        }

        // =========================================================
        // THÊM TOUR - GET
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadDestinations();

            var tour = new Tour
            {
                Status = "Active",
                CreatedAt = DateTime.Now
            };

            return View(tour);
        }

        // =========================================================
        // THÊM TOUR - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Tour tour)
        {
            // Kiểm tra Destination
            var destinationExists = await _context.Destinations
                .AnyAsync(d => d.Id == tour.DestinationId);

            if (!destinationExists)
            {
                ModelState.AddModelError(
                    "DestinationId",
                    "Điểm đến không tồn tại.");
            }

            // Kiểm tra giá
            if (tour.SalePrice.HasValue &&
                tour.SalePrice.Value > tour.Price)
            {
                ModelState.AddModelError(
                    "SalePrice",
                    "Giá khuyến mãi không được lớn hơn giá gốc.");
            }

            // Kiểm tra số chỗ
            if (tour.AvailablePeople > tour.MaxPeople)
            {
                ModelState.AddModelError(
                    "AvailablePeople",
                    "Số chỗ còn lại không được lớn hơn số người tối đa.");
            }

            if (!ModelState.IsValid)
            {
                await LoadDestinations();
                return View(tour);
            }

            // =========================
            // GIÁ TRỊ MẶC ĐỊNH
            // =========================

            tour.CreatedAt = DateTime.Now;

            if (string.IsNullOrWhiteSpace(tour.Status))
            {
                tour.Status = "Active";
            }

            // Nếu chưa nhập AvailablePeople,
            // mặc định bằng MaxPeople.
            if (tour.AvailablePeople == 0 &&
                tour.MaxPeople > 0)
            {
                tour.AvailablePeople = tour.MaxPeople;
            }

            // =========================
            // SLUG
            // =========================

            if (string.IsNullOrWhiteSpace(tour.Slug))
            {
                tour.Slug = GenerateSlug(tour.Name);
            }

            _context.Tours.Add(tour);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Thêm tour thành công.";

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // SỬA TOUR - GET
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var tour = await _context.Tours
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tour == null)
            {
                return NotFound();
            }

            await LoadDestinations();

            return View(tour);
        }

        // =========================================================
        // SỬA TOUR - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            Tour tour)
        {
            if (id != tour.Id)
            {
                return NotFound();
            }

            // =========================
            // KIỂM TRA DESTINATION
            // =========================

            var destinationExists = await _context.Destinations
                .AnyAsync(d => d.Id == tour.DestinationId);

            if (!destinationExists)
            {
                ModelState.AddModelError(
                    "DestinationId",
                    "Điểm đến không tồn tại.");
            }

            // =========================
            // KIỂM TRA GIÁ
            // =========================

            if (tour.SalePrice.HasValue &&
                tour.SalePrice.Value > tour.Price)
            {
                ModelState.AddModelError(
                    "SalePrice",
                    "Giá khuyến mãi không được lớn hơn giá gốc.");
            }

            // =========================
            // KIỂM TRA SỐ CHỖ
            // =========================

            if (tour.AvailablePeople > tour.MaxPeople)
            {
                ModelState.AddModelError(
                    "AvailablePeople",
                    "Số chỗ còn lại không được lớn hơn số người tối đa.");
            }

            if (!ModelState.IsValid)
            {
                await LoadDestinations();
                return View(tour);
            }

            try
            {
                var existingTour = await _context.Tours
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (existingTour == null)
                {
                    return NotFound();
                }

                // =========================
                // CẬP NHẬT THÔNG TIN
                // =========================

                existingTour.Name = tour.Name;
                existingTour.Slug = string.IsNullOrWhiteSpace(tour.Slug)
                    ? GenerateSlug(tour.Name)
                    : tour.Slug;

                existingTour.Description = tour.Description;

                existingTour.Price = tour.Price;
                existingTour.SalePrice = tour.SalePrice;

                existingTour.Duration = tour.Duration;

                existingTour.DepartureLocation =
                    tour.DepartureLocation;

                existingTour.DepartureDate =
                    tour.DepartureDate;

                existingTour.MaxPeople =
                    tour.MaxPeople;

                existingTour.AvailablePeople =
                    tour.AvailablePeople;

                existingTour.ImageUrl =
                    tour.ImageUrl;

                existingTour.DestinationId =
                    tour.DestinationId;

                existingTour.Status =
                    string.IsNullOrWhiteSpace(tour.Status)
                        ? "Active"
                        : tour.Status;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    "Cập nhật tour thành công.";

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                var exists = await _context.Tours
                    .AnyAsync(t => t.Id == id);

                if (!exists)
                {
                    return NotFound();
                }

                throw;
            }
        }

        // =========================================================
        // XÓA TOUR - GET
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var tour = await _context.Tours
                .Include(t => t.Destination)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tour == null)
            {
                return NotFound();
            }

            return View(tour);
        }

        // =========================================================
        // XÓA TOUR - POST
        // =========================================================

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var tour = await _context.Tours
                .Include(t => t.Bookings)
                .Include(t => t.Reviews)
                .Include(t => t.Favorites)
                .Include(t => t.Images)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tour == null)
            {
                return NotFound();
            }

            // Không cho xóa nếu đã có booking
            if (tour.Bookings.Any())
            {
                TempData["ErrorMessage"] =
                    "Không thể xóa tour đã phát sinh booking. " +
                    "Hãy chuyển trạng thái sang Inactive thay vì xóa.";

                return RedirectToAction(nameof(Index));
            }

            _context.TourImages.RemoveRange(tour.Images);
            _context.Reviews.RemoveRange(tour.Reviews);
            _context.Favorites.RemoveRange(tour.Favorites);

            _context.Tours.Remove(tour);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Xóa tour thành công.";

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // ĐỔI TRẠNG THÁI TOUR
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var tour = await _context.Tours
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tour == null)
            {
                return NotFound();
            }

            tour.Status = tour.Status == "Active"
                ? "Inactive"
                : "Active";

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                tour.Status == "Active"
                    ? "Đã kích hoạt tour."
                    : "Đã tạm ngưng tour.";

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // LOAD DESTINATION
        // =========================================================

        private async Task LoadDestinations()
        {
            ViewBag.Destinations = await _context.Destinations
                .OrderBy(d => d.Name)
                .ToListAsync();
        }

        // =========================================================
        // TẠO SLUG
        // =========================================================

        private static string GenerateSlug(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            text = text.Trim().ToLowerInvariant();

            var normalized = text.Normalize(
                System.Text.NormalizationForm.FormD);

            var chars = normalized
                .Where(c =>
                    System.Globalization.CharUnicodeInfo
                        .GetUnicodeCategory(c)
                    != System.Globalization.UnicodeCategory.NonSpacingMark)
                .ToArray();

            var result = new string(chars)
                .Normalize(System.Text.NormalizationForm.FormC);

            result = result
                .Replace('đ', 'd')
                .Replace('Đ', 'd');

            var slugChars = result
                .Select(c =>
                    char.IsLetterOrDigit(c)
                        ? c
                        : '-')
                .ToArray();

            result = new string(slugChars);

            while (result.Contains("--"))
            {
                result = result.Replace("--", "-");
            }

            return result.Trim('-');
        }
    }
}