using FixItNepal.Data;
using FixItNepal.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FixItNepal.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class MapsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public MapsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("providers")]
        public async Task<IActionResult> GetProviders(double? lat, double? lng, double radius = 10, int minRating = 0, int? categoryId = null)
        {
            // Fetch approved providers with valid location
            var query = _context.ServiceProviders
                .Include(p => p.User)
                .Include(p => p.ServiceCategory)
                .Where(p => p.Status == VerificationStatus.Approved 
                            && p.Latitude.HasValue 
                            && p.Longitude.HasValue);

            if (categoryId.HasValue)
            {
                query = query.Where(p => p.ServiceCategoryId == categoryId.Value);
            }

            if (minRating > 0)
            {
                query = query.Where(p => p.AverageRating >= minRating);
            }

            var providers = await query
                .Select(p => new
                {
                    Id = p.Id,
                    Name = p.User.FullName,
                    Category = p.ServiceCategory.Name,
                    Lat = p.Latitude.Value,
                    Lng = p.Longitude.Value,
                    Rating = p.AverageRating,
                    Icon = p.ServiceCategory.IconPath
                })
                .ToListAsync();

            // Client-side distance filtering (simplest for now without spatial DB types)
            if (lat.HasValue && lng.HasValue)
            {
                providers = providers.Where(p => CalculateDistance(lat.Value, lng.Value, p.Lat, p.Lng) <= radius).ToList();
            }

            return Ok(providers);
        }

        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            var R = 6371; // Radius of the earth in km
            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);
            var a =
                Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            var d = R * c; // Distance in km
            return d;
        }

        private double ToRadians(double deg)
        {
            return deg * (Math.PI / 180);
        }
    }
}
