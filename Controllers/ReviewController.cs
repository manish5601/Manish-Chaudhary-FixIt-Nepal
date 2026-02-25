using FixItNepal.Data;
using FixItNepal.Models;
using FixItNepal.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FixItNepal.Controllers
{
    [Authorize]
    public class ReviewController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReviewController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Review/Create/5
        public async Task<IActionResult> Create(int bookingId)
        {
            var userId = _userManager.GetUserId(User);
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == userId);

            if (customer == null) return Unauthorized();

            var booking = await _context.Bookings
                .Include(b => b.ServiceProvider)
                .ThenInclude(p => p.User)
                .Include(b => b.ServiceItem)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null) return NotFound();
            if (booking.CustomerId != customer.Id) return Forbid();
            if (booking.Status != BookingStatus.Completed) return BadRequest("Booking must be completed before reviewing.");

            // Check if already reviewed
            var existingReview = await _context.Reviews.AnyAsync(r => r.BookingId == bookingId);
            if (existingReview) return BadRequest("This booking has already been reviewed.");

            var model = new ReviewViewModel
            {
                BookingId = bookingId,
                ServiceProviderId = booking.ServiceProviderId,
                ServiceProviderName = booking.ServiceProvider.User.FullName,
                ServiceName = booking.ServiceItem.Name
            };

            return View(model);
        }

        // POST: /Review/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ReviewViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var userId = _userManager.GetUserId(User);
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == userId);

            if (customer == null) return Unauthorized();

            var booking = await _context.Bookings.FindAsync(model.BookingId);
            if (booking == null) return NotFound();

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
                
                allReviews.Add(model.Rating);

                provider.TotalReviews = allReviews.Count;
                provider.AverageRating = (decimal)allReviews.Average();
            }

            await _context.SaveChangesAsync();

            return RedirectToAction("Details", "Booking", new { id = model.BookingId });
        }
    }
}
