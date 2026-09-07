using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelAI.Data;
using TravelAI.Models;

namespace TravelAI.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminTourImageController : Controller
    {
        private readonly TravelDbContext _context;

        public AdminTourImageController(TravelDbContext context)
        {
            _context = context;
        }

        // Danh sách ảnh của Tour
        public async Task<IActionResult> Index(int tourId)
        {
            var tour = await _context.Tours
                .Include(t => t.Images)
                .FirstOrDefaultAsync(t => t.Id == tourId);

            if (tour == null)
            {
                return NotFound();
            }

            ViewBag.Tour = tour;

            var images = await _context.TourImages
                .Where(i => i.TourId == tourId)
                .OrderBy(i => i.SortOrder)
                .ThenBy(i => i.Id)
                .ToListAsync();

            return View(images);
        }

        // Thêm ảnh
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            int tourId,
            string imageUrl,
            int sortOrder = 0)
        {
            var tourExists = await _context.Tours
                .AnyAsync(t => t.Id == tourId);

            if (!tourExists)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(imageUrl))
            {
                TempData["Error"] = "Vui lòng nhập URL hình ảnh.";
                return RedirectToAction(nameof(Index), new { tourId });
            }

            if (imageUrl.Length > 1000)
            {
                TempData["Error"] = "URL hình ảnh không được vượt quá 1000 ký tự.";
                return RedirectToAction(nameof(Index), new { tourId });
            }

            if (sortOrder < 0)
            {
                sortOrder = 0;
            }

            var image = new TourImage
            {
                TourId = tourId,
                ImageUrl = imageUrl.Trim(),
                SortOrder = sortOrder
            };

            _context.TourImages.Add(image);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã thêm hình ảnh thành công.";

            return RedirectToAction(nameof(Index), new { tourId });
        }

        // Cập nhật ảnh
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            string imageUrl,
            int sortOrder)
        {
            var image = await _context.TourImages
                .FirstOrDefaultAsync(i => i.Id == id);

            if (image == null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(imageUrl))
            {
                TempData["Error"] = "URL hình ảnh không được để trống.";

                return RedirectToAction(
                    nameof(Index),
                    new { tourId = image.TourId });
            }

            if (imageUrl.Length > 1000)
            {
                TempData["Error"] = "URL hình ảnh không được vượt quá 1000 ký tự.";

                return RedirectToAction(
                    nameof(Index),
                    new { tourId = image.TourId });
            }

            image.ImageUrl = imageUrl.Trim();
            image.SortOrder = Math.Max(0, sortOrder);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã cập nhật hình ảnh.";

            return RedirectToAction(
                nameof(Index),
                new { tourId = image.TourId });
        }

        // Xóa ảnh
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var image = await _context.TourImages
                .FirstOrDefaultAsync(i => i.Id == id);

            if (image == null)
            {
                return NotFound();
            }

            var tourId = image.TourId;

            _context.TourImages.Remove(image);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã xóa hình ảnh.";

            return RedirectToAction(
                nameof(Index),
                new { tourId });
        }

        // Đưa ảnh lên đầu danh sách
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MoveUp(int id)
        {
            var image = await _context.TourImages
                .FirstOrDefaultAsync(i => i.Id == id);

            if (image == null)
            {
                return NotFound();
            }

            var previousImage = await _context.TourImages
                .Where(i =>
                    i.TourId == image.TourId &&
                    i.SortOrder < image.SortOrder)
                .OrderByDescending(i => i.SortOrder)
                .FirstOrDefaultAsync();

            if (previousImage != null)
            {
                var currentOrder = image.SortOrder;

                image.SortOrder = previousImage.SortOrder;
                previousImage.SortOrder = currentOrder;

                await _context.SaveChangesAsync();
            }

            return RedirectToAction(
                nameof(Index),
                new { tourId = image.TourId });
        }

        // Đưa ảnh xuống dưới
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MoveDown(int id)
        {
            var image = await _context.TourImages
                .FirstOrDefaultAsync(i => i.Id == id);

            if (image == null)
            {
                return NotFound();
            }

            var nextImage = await _context.TourImages
                .Where(i =>
                    i.TourId == image.TourId &&
                    i.SortOrder > image.SortOrder)
                .OrderBy(i => i.SortOrder)
                .FirstOrDefaultAsync();

            if (nextImage != null)
            {
                var currentOrder = image.SortOrder;

                image.SortOrder = nextImage.SortOrder;
                nextImage.SortOrder = currentOrder;

                await _context.SaveChangesAsync();
            }

            return RedirectToAction(
                nameof(Index),
                new { tourId = image.TourId });
        }
    }
}