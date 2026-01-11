using FixItNepal.Data;
using FixItNepal.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FixItNepal.Controllers.Api
{
    [Route("api/[controller]")]
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
        public IActionResult GetCategories()
        {
            var categories = Enum.GetValues(typeof(ServiceCategory))
                                 .Cast<ServiceCategory>()
                                 .Select(c => new { Id = (int)c, Name = c.ToString() })
                                 .ToList();
            return Ok(categories);
        }

        // GET: api/services/providers
        [HttpGet("providers")]
        public async Task<IActionResult> GetProviders()
        {
            var providers = await _context.ServiceProviders
                .Include(p => p.User)
                .Where(p => p.Status == VerificationStatus.Approved)
                .Select(p => new
                {
                    p.Id,
                    p.User.FullName,
                    Service = p.PrimaryService.ToString(),
                    Rating = p.AverageRating,
                    p.ExperienceYears,
                    p.ServiceAreas,
                    p.User.ProfilePicture
                })
                .ToListAsync();

            return Ok(providers);
        }
    }
}
