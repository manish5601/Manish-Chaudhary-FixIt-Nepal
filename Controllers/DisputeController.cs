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
    public class DisputeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DisputeController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
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
            var booking = await _context.Bookings.FindAsync(model.BookingId);
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

            _context.Disputes.Add(dispute);
            await _context.SaveChangesAsync();

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
