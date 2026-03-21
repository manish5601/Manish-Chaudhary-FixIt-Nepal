using FixItNepal.Data;
using FixItNepal.Models;
using FixItNepal.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FixItNepal.Services;

namespace FixItNepal.Controllers
{
    [Authorize]
    public class DisputeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;

        public DisputeController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IEmailService emailService)
        {
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
        }

        // GET: /Dispute/Create?bookingId=5
        public async Task<IActionResult> Create(int bookingId)
        {
            var userId = _userManager.GetUserId(User);
            var booking = await _context.Bookings
                .Include(b => b.Customer)
                .ThenInclude(c => c.User)
                .Include(b => b.ServiceProvider)
                .ThenInclude(p => p.User)
                .Include(b => b.ServiceItem)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null) return NotFound();
            
            // Authorization
            if (booking.Customer.UserId != userId && booking.ServiceProvider.UserId != userId)
            {
                return Forbid();
            }

            var otherPartyName = booking.Customer.UserId == userId ? booking.ServiceProvider.User.FullName : booking.Customer.User.FullName;

            var model = new DisputeViewModel
            {
                BookingId = bookingId,
                ServiceName = booking.ServiceItem.Name,
                PartyName = otherPartyName
            };

            return View(model);
        }

        // POST: /Dispute/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DisputeViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var userId = _userManager.GetUserId(User);
            var booking = await _context.Bookings
                .Include(b => b.Customer).ThenInclude(c => c.User)
                .Include(b => b.ServiceProvider).ThenInclude(p => p.User)
                .Include(b => b.ServiceItem)
                .FirstOrDefaultAsync(b => b.Id == model.BookingId);

            if (booking == null) return NotFound();

            var dispute = new Dispute
            {
                BookingId = model.BookingId,
                RaisedById = userId,
                Reason = model.Reason,
                Description = model.Description,
                Status = DisputeStatus.Open,
                CreatedAt = DateTime.UtcNow
            };

            // Notify other party
            var isCustomer = booking.Customer.UserId == userId;
            var targetUserId = isCustomer ? booking.ServiceProvider.UserId : booking.Customer.UserId;
            var targetUser = await _userManager.FindByIdAsync(targetUserId);

            if (targetUser != null)
            {
                var notif = new Notification
                {
                    UserId = targetUserId,
                    Title = "Dispute Raised",
                    Message = $"A dispute has been raised regarding the booking for {booking.ServiceItem?.Name}.",
                    Type = NotificationType.System,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow,
                    RelatedEntityId = booking.Id,
                    RelatedEntityType = "Booking"
                };
                _context.Notifications.Add(notif);

                // Send Email to other party
                var subject = $"Dispute Raised: Booking #{booking.Id} - FixIt Nepal";
                var body = $@"
                    <div style='font-family: sans-serif; color: #333;'>
                        <h2 style='color: #dc3545;'>Dispute Notification</h2>
                        <p>Hello <strong>{targetUser.FullName}</strong>,</p>
                        <p>A dispute has been raised regarding your booking for <strong>{booking.ServiceItem?.Name}</strong>.</p>
                        <div style='background-color: #f8f9fa; padding: 15px; border-radius: 8px; margin: 20px 0;'>
                            <h4 style='margin-top: 0;'>Dispute Details:</h4>
                            <ul style='list-style: none; padding: 0;'>
                                <li><strong>Booking ID:</strong> #{booking.Id}</li>
                                <li><strong>Reason:</strong> {model.Reason}</li>
                                <li><strong>Description:</strong> {model.Description}</li>
                            </ul>
                        </div>
                        <p>Our support team will review the dispute and contact you soon if needed. You can view the booking details here: <a href='https://fixitnepal.com/Booking/Details/{booking.Id}'>Booking Details</a>.</p>
                        <hr style='border: 0; border-top: 1px solid #eee; margin: 20px 0;'>
                        <p style='font-size: 0.9em; color: #666;'>Thank you for your patience and for using FixIt Nepal.</p>
                    </div>
                ";
                await _emailService.SendEmailAsync(targetUser.Email, subject, body);
                await _context.SaveChangesAsync();
            }

            TempData["SuccessMessage"] = "Dispute raised successfully. Our support team will review it.";
            return RedirectToAction("Details", "Booking", new { id = model.BookingId });
        }

        // GET: /Dispute/MyDisputes
        public async Task<IActionResult> MyDisputes()
        {
            var userId = _userManager.GetUserId(User);
            var disputes = await _context.Disputes
                .Include(d => d.Booking)
                .ThenInclude(b => b.ServiceItem)
                .Where(d => d.RaisedById == userId)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();

            return View(disputes);
        }
    }
}
