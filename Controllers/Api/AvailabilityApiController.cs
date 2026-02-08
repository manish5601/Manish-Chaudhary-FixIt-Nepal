using FixItNepal.Data;
using FixItNepal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FixItNepal.Controllers.Api
{
    [Route("api/availability")]
    [ApiController]
    [Authorize(AuthenticationSchemes = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme, Roles = "ServiceProvider")]
    public class AvailabilityApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AvailabilityApiController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProviderAvailability>>> GetAvailability()
        {
            var userId = _userManager.GetUserId(User);
            var provider = await _context.ServiceProviders.FirstOrDefaultAsync(p => p.UserId == userId);
            
            if (provider == null) return NotFound("Provider profile not found.");

            return await _context.ProviderAvailabilities
                .Where(a => a.ServiceProviderId == provider.Id)
                .ToListAsync();
        }

        [HttpPost]
        public async Task<ActionResult<ProviderAvailability>> SetAvailability(ProviderAvailability availability)
        {
            var userId = _userManager.GetUserId(User);
            var provider = await _context.ServiceProviders.FirstOrDefaultAsync(p => p.UserId == userId);

            if (provider == null) return NotFound("Provider profile not found.");

            availability.ServiceProviderId = provider.Id;

            // Check if availability for this day already exists
            var existing = await _context.ProviderAvailabilities
                .FirstOrDefaultAsync(a => a.ServiceProviderId == provider.Id && a.DayOfWeek == availability.DayOfWeek);

            if (existing != null)
            {
                existing.StartTime = availability.StartTime;
                existing.EndTime = availability.EndTime;
                existing.IsDayOff = availability.IsDayOff;
            }
            else
            {
                _context.ProviderAvailabilities.Add(availability);
            }

            await _context.SaveChangesAsync();

            return Ok(availability);
        }
    }
}
