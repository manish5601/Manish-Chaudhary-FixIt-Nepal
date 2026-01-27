using System.Diagnostics;
using FixItNepal.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FixItNepal.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly FixItNepal.Data.ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, FixItNepal.Data.ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Search(string query, int? categoryId, double? minRating, double? lat, double? lng, double maxDistanceKm = 10)
        {
            var providersQuery = _context.ServiceProviders
                .Include(p => p.User)
                .Include(p => p.ServiceCategory)
                .Where(p => p.Status == VerificationStatus.Approved);

            if (!string.IsNullOrEmpty(query))
            {
                providersQuery = providersQuery.Where(p => 
                    p.User.FullName.Contains(query) || 
                    p.Skills.Contains(query) || 
                    p.ServiceCategory.Name.Contains(query));
            }

            if (categoryId.HasValue)
            {
                providersQuery = providersQuery.Where(p => p.ServiceCategoryId == categoryId);
            }

            if (minRating.HasValue)
            {
                providersQuery = providersQuery.Where(p => p.AverageRating >= (decimal)minRating);
            }

            var providers = await providersQuery.ToListAsync();

            // Client-side distance filtering (for simplicity without spatial DB)
            if (lat.HasValue && lng.HasValue)
            {
                providers = providers.Where(p => 
                {
                    if (!p.Latitude.HasValue || !p.Longitude.HasValue) return false;
                    var dist = GetDistance(lat.Value, lng.Value, p.Latitude.Value, p.Longitude.Value);
                    return dist <= maxDistanceKm;
                }).ToList();
            }

            ViewBag.Categories = _context.ServiceCategories.Where(c => c.IsActive).ToList();
            ViewBag.Query = query;
            ViewBag.CategoryId = categoryId;
            ViewBag.MinRating = minRating;

            return View(providers);
        }

        private double GetDistance(double lat1, double lon1, double lat2, double lon2)
        {
            var R = 6371; // Radius of the earth in km
            var dLat = Deg2Rad(lat2 - lat1);
            var dLon = Deg2Rad(lon2 - lon1);
            var a =
                Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(Deg2Rad(lat1)) * Math.Cos(Deg2Rad(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            var d = R * c; // Distance in km
            return d;
        }

        private double Deg2Rad(double deg)
        {
            return deg * (Math.PI / 180);
        }

        public async Task<IActionResult> Index()
        {
            var categories = await _context.ServiceCategories
                .Where(c => c.IsActive)
                .Take(6)
                .ToListAsync();
            return View(categories);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
