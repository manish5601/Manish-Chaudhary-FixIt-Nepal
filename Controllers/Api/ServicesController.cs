using FixItNepal.Data;
using FixItNepal.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FixItNepal.Controllers.Api
{
    [Route("api/services")]
    [ApiController]
    public class ServicesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ServicesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/services/categories
        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories()
        {
            var categories = await _context.ServiceCategories
                                 .Where(c => c.IsActive)
                                 .Select(c => new { c.Id, c.Name })
                                 .ToListAsync();
            return Ok(categories);
        }

        // GET: api/services/providers
        [HttpGet("providers")]
        public async Task<IActionResult> GetProviders()
        {
            var providers = await _context.ServiceProviders
                .Include(p => p.User)
                .Include(p => p.ServiceCategory)
                .Where(p => p.Status == VerificationStatus.Approved)
                .Select(p => new
                {
                    p.Id,
                    p.User.FullName,
                    Service = p.ServiceCategory.Name,
                    Rating = p.AverageRating,
                    p.ExperienceYears,
                    p.ServiceAreas,
                    p.User.ProfilePicture
                })
                .ToListAsync();

            return Ok(providers);
        }
        // GET: api/services/items
        [HttpGet("items")]
        public async Task<IActionResult> GetServiceItems()
        {
            var items = await _context.ServiceItems
                                 .Where(i => i.IsActive)
                                 .Select(i => new { i.Id, i.Name, i.Description, i.BasePrice, Category = i.ServiceCategory.Name })
                                 .ToListAsync();
            return Ok(items);
        }
    }
}
