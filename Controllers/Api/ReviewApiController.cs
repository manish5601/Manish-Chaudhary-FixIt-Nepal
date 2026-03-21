using FixItNepal.Data;
using FixItNepal.Models;
using FixItNepal.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using FixItNepal.Services;

namespace FixItNepal.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "Bearer,Identity.Application")]
    public class ReviewApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;

        public ReviewApiController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IEmailService emailService)
        {
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
        }

        // POST: api/ReviewApi
        [HttpPost]
        public async Task<IActionResult> SubmitReview([FromBody] ReviewViewModel model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userId = _userManager.GetUserId(User);
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == userId);

            if (customer == null) return BadRequest("Only customers can leave reviews.");

            var booking = await _context.Bookings
                .Include(b => b.ServiceProvider).ThenInclude(p => p.User)
                .FirstOrDefaultAsync(b => b.Id == model.BookingId);

            if (booking == null) return NotFound("Booking not found.");

            if (booking.CustomerId != customer.Id) return Forbid("You can only review your own bookings.");

            if (booking.Status != BookingStatus.Completed)
                return BadRequest("You can only review completed bookings.");

            // Check if review already exists
            var existingReview = await _context.Reviews.AnyAsync(r => r.BookingId == model.BookingId);
            if (existingReview) return BadRequest("This booking has already been reviewed.");

            var review = new Review
            {
                BookingId = model.BookingId,
                CustomerId = customer.Id,
                ServiceProviderId = booking.ServiceProviderId,
                Rating = model.Rating,
                Comment = model.Comment,
                CreatedAt = DateTime.UtcNow
            };

            _context.Reviews.Add(review);

            // Update Provider Stats
            var provider = await _context.ServiceProviders.FindAsync(booking.ServiceProviderId);
            if (provider != null)
            {
                var allReviews = await _context.Reviews
                    .Where(r => r.ServiceProviderId == provider.Id)
                    .Select(r => r.Rating)
                    .ToListAsync();

                allReviews.Add(model.Rating); // Add current rating to calculation

                provider.TotalReviews = allReviews.Count;
                provider.AverageRating = (decimal)allReviews.Average();
            }

            // Notify Provider
            var notif = new Notification
            {
                UserId = booking.ServiceProvider.UserId,
                Title = "New Review Received",
                Message = $"A customer has left a {model.Rating}-star review for your {booking.ServiceItem?.Name} service.",
                RelatedEntityId = booking.Id,
                RelatedEntityType = "Booking",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };
            _context.Notifications.Add(notif);

            // Send Email to Provider
            var providerUser = booking.ServiceProvider.User;
            if (providerUser != null)
            {
                var subject = $"New Review Received: {model.Rating} Stars! - FixIt Nepal";
                var body = $@"
                    <div style='font-family: sans-serif; color: #333;'>
                        <h2 style='color: #ffca28;'>New Review Received!</h2>
                        <p>Hello <strong>{providerUser.FullName}</strong>,</p>
                        <p>A customer has left a <strong>{model.Rating}-star</strong> review for your <strong>{booking.ServiceItem?.Name}</strong> service.</p>
                        <div style='background-color: #f8f9fa; padding: 15px; border-radius: 8px; margin: 20px 0;'>
                            <p style='font-style: italic;'>""{model.Comment}""</p>
                        </div>
                        <p>You can view all your reviews on your <a href='https://fixitnepal.com/ServiceProvider/Profile'>Profile</a>.</p>
                        <hr style='border: 0; border-top: 1px solid #eee; margin: 20px 0;'>
                        <p style='font-size: 0.9em; color: #666;'>Keep up the great work! <br> FixIt Nepal Team</p>
                    </div>
                ";
                await _emailService.SendEmailAsync(providerUser.Email, subject, body);
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Review submitted successfully", reviewId = review.Id });
        }

        // GET: api/ReviewApi/provider/{providerId}
        [HttpGet("provider/{providerId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetProviderReviews(int providerId)
        {
            var provider = await _context.ServiceProviders.FindAsync(providerId);
            if (provider == null)
            {
                return NotFound(new { message = "Provider not found" });
            }

            var reviews = await _context.Reviews
                .Include(r => r.Customer)
                .ThenInclude(c => c.User)
                .Where(r => r.ServiceProviderId == providerId && !r.IsFlagged)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new
                {
                    r.Id,
                    CustomerName = r.Customer.User.FullName,
                    r.Rating,
                    r.Comment,
                    r.CreatedAt
                })
                .ToListAsync();

            if (!reviews.Any())
            {
                return Ok(new { message = "No reviews found for this provider", reviews = new List<object>() });
            }

            return Ok(reviews);
        }

        // POST: api/ReviewApi/flag/{id}
        [HttpPost("flag/{id}")]
        public async Task<IActionResult> FlagReview(int id, [FromBody] string reason)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review == null) return NotFound();

            review.IsFlagged = true;
            review.AdminNote = $"Flagged by user. Reason: {reason}";

            await _context.SaveChangesAsync();

            return Ok(new { message = "Review flagged for moderation." });
        }
    }
}
