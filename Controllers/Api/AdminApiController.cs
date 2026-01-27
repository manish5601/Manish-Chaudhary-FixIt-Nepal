using FixItNepal.Data;
using FixItNepal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FixItNepal.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
    public class AdminApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AdminApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/adminapi/providers/pending
        [HttpGet("providers/pending")]
        public async Task<IActionResult> GetPendingProviders()
        {
            var pendingProviders = await _context.ServiceProviders
                .Include(p => p.User)
                .Include(p => p.ServiceCategory)
                .Where(p => p.Status == VerificationStatus.Pending)
                .Select(p => new
                {
                    p.Id,
                    p.User.FullName,
                    p.User.Email,
                    p.User.PhoneNumber,
                    Service = p.ServiceCategory.Name,
                    p.ExperienceYears,
                    RegisteredAt = p.RegisteredAt
                })
                .ToListAsync();

            return Ok(pendingProviders);
        }

        // POST: api/adminapi/providers/verify/5
        [HttpPost("providers/verify/{id}")]
        public async Task<IActionResult> VerifyProvider(int id)
        {
            var provider = await _context.ServiceProviders.FindAsync(id);

            if (provider == null)
            {
                return NotFound(new { message = "Provider not found" });
            }

            if (provider.Status == VerificationStatus.Approved)
            {
                return BadRequest(new { message = "Provider is already verified" });
            }

            provider.Status = VerificationStatus.Approved;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Provider verified successfully", providerId = provider.Id });
        }
    }
}
