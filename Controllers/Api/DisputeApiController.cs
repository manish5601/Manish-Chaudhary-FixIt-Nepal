using FixItNepal.Data;
using FixItNepal.Models;
using FixItNepal.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FixItNepal.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "Bearer,Identity.Application")]
    public class DisputeApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DisputeApiController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // POST: api/DisputeApi
        [HttpPost]
        public async Task<IActionResult> RaiseDispute([FromBody] DisputeViewModel model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userId = _userManager.GetUserId(User);
            var booking = await _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.ServiceProvider)
                .FirstOrDefaultAsync(b => b.Id == model.BookingId);

            if (booking == null) return NotFound(new { message = "Booking not found." });

            // Authorization: Only Customer or Provider involved in the booking can raise a dispute
            if (booking.Customer.UserId != userId && booking.ServiceProvider.UserId != userId)
            {
                return Forbid("You are not authorized to raise a dispute for this booking.");
            }

            var dispute = new Dispute
            {
                BookingId = model.BookingId,
                RaisedById = userId,
                Reason = model.Reason,
                Description = model.Description,
                Status = DisputeStatus.Open,
                CreatedAt = DateTime.UtcNow
            };

            _context.Disputes.Add(dispute);

            // Notify Admin
            // In a real app, we'd notify all admins or a specific role
            var admins = await _userManager.GetUsersInRoleAsync("Admin");
            foreach (var admin in admins)
            {
                _context.Notifications.Add(new Notification
                {
                    UserId = admin.Id,
                    Title = "New Dispute Raised",
                    Message = $"A dispute has been raised for Booking #{booking.Id} by {User.Identity?.Name}.",
                    RelatedEntityId = dispute.Id,
                    RelatedEntityType = "Dispute"
                });
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Dispute raised successfully", disputeId = dispute.Id });
        }

        // GET: api/DisputeApi/my-disputes
        [HttpGet("my-disputes")]
        public async Task<IActionResult> GetMyDisputes()
        {
            var userId = _userManager.GetUserId(User);
            var disputes = await _context.Disputes
                .Include(d => d.Booking)
                .Where(d => d.RaisedById == userId)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();

            if (!disputes.Any())
            {
                return Ok(new { message = "No disputes found", disputes = new List<object>() });
            }

            return Ok(disputes);
        }
    }
}
