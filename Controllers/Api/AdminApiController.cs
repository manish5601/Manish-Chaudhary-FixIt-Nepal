using FixItNepal.Data;
using FixItNepal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;


namespace FixItNepal.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
    public class AdminApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminApiController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
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

            return Ok(ApiResponse<object>.SuccessResponse(pendingProviders, "Pending providers retrieved successfully"));
        }

        // POST: api/adminapi/providers/verify/5
        [HttpPost("providers/verify/{id}")]
        public async Task<IActionResult> VerifyProvider(int id)
        {
            var provider = await _context.ServiceProviders.FindAsync(id);

            if (provider == null)
            {
                return NotFound(ApiResponse<object>.ErrorResponse("Provider not found"));
            }

            if (provider.Status == VerificationStatus.Approved)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse("Provider is already verified"));
            }

            provider.Status = VerificationStatus.Approved;
            await _context.SaveChangesAsync();

            return Ok(ApiResponse<object>.SuccessResponse(new { providerId = provider.Id }, "Provider verified successfully"));
        }

        // POST: api/adminapi/reviews/dismiss/5
        [HttpPost("reviews/dismiss/{id}")]
        public async Task<IActionResult> DismissReviewFlag(int id)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review == null) return NotFound(ApiResponse<object>.ErrorResponse("Review not found"));

            review.IsFlagged = false;
            review.AdminNote = "Flag dismissed via API.";

            await _context.SaveChangesAsync();
            return Ok(ApiResponse<object>.SuccessResponse(null, "Review flag dismissed"));
        }

        // DELETE: api/adminapi/reviews/5
        [HttpDelete("reviews/{id}")]
        public async Task<IActionResult> DeleteReview(int id)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review == null) return NotFound(ApiResponse<object>.ErrorResponse("Review not found"));

            var providerId = review.ServiceProviderId;
            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();

            // Recalculate Provider Rating
            var provider = await _context.ServiceProviders.FindAsync(providerId);
            if (provider != null)
            {
                var reviews = await _context.Reviews.Where(r => r.ServiceProviderId == providerId).Select(r => r.Rating).ToListAsync();
                if (reviews.Any())
                {
                    provider.TotalReviews = reviews.Count;
                    provider.AverageRating = (decimal)reviews.Average();
                }
                else
                {
                    provider.TotalReviews = 0;
                    provider.AverageRating = 0;
                }
                await _context.SaveChangesAsync();
            }

            return Ok(ApiResponse<object>.SuccessResponse(null, "Review deleted successfully"));
        }
        // GET: api/adminapi/reviews/flagged
        [HttpGet("reviews/flagged")]
        public async Task<IActionResult> GetFlaggedReviews()
        {
            var flaggedReviews = await _context.Reviews
                .Include(r => r.Customer).ThenInclude(c => c.User)
                .Include(r => r.ServiceProvider).ThenInclude(p => p.User)
                .Where(r => r.IsFlagged)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new
                {
                    r.Id,
                    CustomerName = r.Customer.User.FullName,
                    ProviderName = r.ServiceProvider.User.FullName,
                    r.Rating,
                    r.Comment,
                    r.AdminNote,
                    r.CreatedAt
                })
                .ToListAsync();

            return Ok(ApiResponse<object>.SuccessResponse(flaggedReviews, "Flagged reviews retrieved successfully"));
        }

        // GET: api/adminapi/disputes
        [HttpGet("disputes")]
        public async Task<IActionResult> GetDisputes()
        {
            var disputes = await _context.Disputes
                .Include(d => d.RaisedBy)
                .OrderByDescending(d => d.CreatedAt)
                .Select(d => new
                {
                    d.Id,
                    d.BookingId,
                    RaisedBy = d.RaisedBy.FullName,
                    d.Reason,
                    d.Description,
                    Status = d.Status.ToString(),
                    d.CreatedAt
                })
                .ToListAsync();

            return Ok(ApiResponse<object>.SuccessResponse(disputes, "Disputes retrieved successfully"));
        }

        // POST: api/adminapi/disputes/resolve/{id}
        [HttpPost("disputes/resolve/{id}")]
        public async Task<IActionResult> ResolveDispute(int id, [FromBody] ResolveDisputeDto model)
        {
            var dispute = await _context.Disputes
                .Include(d => d.Booking)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (dispute == null) return NotFound(ApiResponse<object>.ErrorResponse("Dispute not found"));

            dispute.Status = model.Status;
            dispute.Resolution = model.Resolution;
            dispute.ResolvedAt = DateTime.UtcNow;
            dispute.ResolvedBy = _userManager.GetUserId(User);

            // Notify party who raised the dispute
            _context.Notifications.Add(new Notification
            {
                UserId = dispute.RaisedById,
                Title = "Dispute Resolved",
                Message = $"Your dispute for Booking #{dispute.BookingId} has been {model.Status}. Resolution: {model.Resolution}",
                RelatedEntityId = dispute.Id,
                RelatedEntityType = "Dispute"
            });

            await _context.SaveChangesAsync();
            return Ok(ApiResponse<object>.SuccessResponse(new { status = dispute.Status.ToString() }, "Dispute resolved successfully"));
        }
    }

    public class ResolveDisputeDto
    {
        public DisputeStatus Status { get; set; }
        public string Resolution { get; set; }
    }
}



