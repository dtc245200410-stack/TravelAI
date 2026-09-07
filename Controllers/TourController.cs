using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelAI.Data;

namespace TravelAI.Controllers
{
    public class TourController : Controller
    {
        private readonly TravelDbContext _context;

        public TourController(TravelDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(
            string? searchString,
            string? destination,
            string? sort,
            int page = 1)
        {
            const int pageSize = 9;

            if (page < 1)
            {
                page = 1;
            }
            var tours = _context.Tours
                .Include(t => t.Destination)
                .Where(t => t.Status == "Active")
                .AsQueryable();

            // ==============================
            // TÌM KIẾM
            // ==============================

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                tours = tours.Where(t =>
                    t.Name.Contains(searchString) ||
                    t.Description.Contains(searchString) ||
                    t.DepartureLocation.Contains(searchString) ||
                    t.Destination.Name.Contains(searchString));
            }

            // ==============================
            // LỌC ĐIỂM ĐẾN
            // ==============================

            if (!string.IsNullOrWhiteSpace(destination))
            {
                tours = tours.Where(t =>
                    t.Destination.Name == destination);
            }

            // ==============================
            // SẮP XẾP
            // ==============================

            tours = sort switch
            {
                "priceAsc" => tours.OrderBy(t =>
                    t.SalePrice ?? t.Price),

                "priceDesc" => tours.OrderByDescending(t =>
                    t.SalePrice ?? t.Price),

                "rating" => tours.OrderByDescending(t =>
                    t.Rating),

                _ => tours.OrderByDescending(t =>
                    t.Rating)
            };

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

            var destinations = await _context.Destinations
                .OrderBy(d => d.Name)
                .ToListAsync();

            ViewBag.Destinations = destinations;
            ViewBag.SearchString = searchString;
            ViewBag.SelectedDestination = destination;
            ViewBag.SelectedSort = sort;

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalTours = totalTours;

            return View(result);
        }
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var tour = await _context.Tours
                .Include(t => t.Destination)
                .Include(t => t.Images)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tour == null)
            {
                return NotFound();
            }

            return View(tour);
        }
    }
}