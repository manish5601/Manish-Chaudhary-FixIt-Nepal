using FixItNepal.Data;
using FixItNepal.Models;
using FixItNepal.ViewModels;
using FixItNepal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FixItNepal.Controllers
{
    [Authorize]
    public class BookingController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;

        public BookingController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IEmailService emailService)
        {
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
        }

        // GET: Booking/Create?serviceId=5&providerId=10
        public async Task<IActionResult> Create(int serviceId, int providerId)
        {
            var service = await _context.ServiceItems.FindAsync(serviceId);
            var provider = await _context.ServiceProviders.Include(p => p.User).FirstOrDefaultAsync(p => p.Id == providerId);

            if (service == null || provider == null)
            {
                return NotFound();
            }

            var model = new BookingViewModel
            {
                ServiceItemId = serviceId,
                ServiceName = service.Name,
                ServiceProviderId = providerId,
                ProviderName = provider.User.FullName,
                Price = service.BasePrice,
                BookingDate = DateTime.Today.AddDays(1) // Default to tomorrow
            };

            return View(model);
        }

        // POST: Booking/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BookingViewModel model)
        {
            if (ModelState.IsValid)
            {
                var userId = _userManager.GetUserId(User);
                var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == userId);

                if (customer == null)
                {
                    return RedirectToAction("RegisterCustomer", "Auth"); 
                }

                // Call API logic here or reuse service? 
                // For simplified architecture, we'll duplicate simplified logic or call the internal service if we had one.
                // Since verified API exists, let's use the Context directly but ideally we should use the API/Service.
                // Keeping it direct for now as per previous pattern.

                var booking = new Booking
                {
                    CustomerId = customer.Id,
                    ServiceProviderId = model.ServiceProviderId,
                    ServiceItemId = model.ServiceItemId,
                    BookingDate = model.BookingDate,
                    StartTime = model.StartTime,
                    EndTime = model.EndTime,
                    Status = BookingStatus.Pending,
                    TotalPrice = model.Price, // In real app, calculate based on hours * rate
                    Notes = model.Notes,
                    CustomerAddress = model.CustomerAddress,
                    CustomerPhone = model.CustomerPhone
                };

                _context.Bookings.Add(booking);
                
                // Add Notification for Provider
                var providerUser = await _context.ServiceProviders.Include(p => p.User).FirstOrDefaultAsync(p => p.Id == model.ServiceProviderId);
                if (providerUser != null)
                {
                    var notif = new Notification
                    {
                        UserId = providerUser.UserId,
                        Title = "New Booking Request",
                        Message = $"You have a new booking request from a customer for {model.BookingDate.ToShortDateString()}.",
                        Type = NotificationType.System,
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Notifications.Add(notif);

                    // Send Email to Provider
                    var subject = "New Booking Request - FixIt Nepal";
                    var body = $@"
                        <h2>New Booking Request</h2>
                        <p>Hello {providerUser.User.FullName},</p>
                        <p>You have received a new booking request for <strong>{model.ServiceName}</strong>.</p>
                        <p><strong>Date:</strong> {model.BookingDate.ToShortDateString()}</p>
                        <p><strong>Time:</strong> {model.StartTime} - {model.EndTime}</p>
                        <p>Please login to your dashboard to review and accept the booking.</p>
                    ";
                    await _emailService.SendEmailAsync(providerUser.User.Email, subject, body);
                }

                await _context.SaveChangesAsync();

                return RedirectToAction("MyBookings");
            }

            return View(model);
        }

        // GET: Booking/MyBookings
        public async Task<IActionResult> MyBookings()
        {
            var userId = _userManager.GetUserId(User);
            var isProvider = User.IsInRole("ServiceProvider");
            
            if (isProvider)
            {
                 var provider = await _context.ServiceProviders.FirstOrDefaultAsync(p => p.UserId == userId);
                 if (provider == null) return View(new List<Booking>());

                 var bookings = await _context.Bookings
                     .Include(b => b.Customer).ThenInclude(c => c.User)
                     .Include(b => b.ServiceItem)
                     .Where(b => b.ServiceProviderId == provider.Id)
                     .OrderByDescending(b => b.BookingDate)
                     .ToListAsync();
                 
                 ViewBag.IsProvider = true;
                 return View(bookings);
            }
            else
            {
                var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == userId);
                if (customer == null) return View(new List<Booking>());

                var bookings = await _context.Bookings
                    .Include(b => b.ServiceProvider).ThenInclude(p => p.User)
                    .Include(b => b.ServiceItem)
                    .Where(b => b.CustomerId == customer.Id)
                    .OrderByDescending(b => b.BookingDate)
                    .ToListAsync();

                 ViewBag.IsProvider = false;
                 return View(bookings);
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int id, BookingStatus status)
        {
            var booking = await _context.Bookings.Include(b => b.Customer).Include(b => b.ServiceProvider).FirstOrDefaultAsync(b => b.Id == id);
            if (booking == null) return NotFound();

            var userId = _userManager.GetUserId(User);
            
            // Authorization check (Basic)
            // Provider can Accept/Reject. Customer can Cancel.
            
            booking.Status = status;
            
            // Notify other party
            string targetUserId = "";
            string message = "";
            
            if (User.IsInRole("ServiceProvider"))
            {
                targetUserId = booking.Customer.UserId;
                message = $"Your booking for {booking.ServiceItem?.Name} has been {status}.";
            }
            else
            {
                // Customer Cancellation Logic
                if (booking.Status == BookingStatus.Confirmed)
                {
                    // Prevent customer from cancelling if already confirmed
                    TempData["ErrorMessage"] = "You cannot cancel a confirmed booking. Please contact support or the provider.";
                    return RedirectToAction("Details", new { id = booking.Id });
                }

                targetUserId = booking.ServiceProvider.UserId;
                message = $"Booking for {booking.ServiceItem?.Name} has been {status} by customer.";
            }

            var notif = new Notification
            {
                UserId = targetUserId,
                Title = $"Booking {status}",
                Message = message,
                Type = NotificationType.System,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };
            _context.Notifications.Add(notif);

            // Send Email Notification
            var targetUser = await _userManager.FindByIdAsync(targetUserId);
            if (targetUser != null)
            {
                 var subject = $"Booking Update - {status}";
                 var body = $@"
                     <h2>Booking Update</h2>
                     <p>Hello {targetUser.FullName},</p>
                     <p>{message}</p>
                     <p>Thank you for using FixIt Nepal.</p>
                 ";
                 await _emailService.SendEmailAsync(targetUser.Email, subject, body);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("MyBookings");
        }

        // GET: Booking/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var booking = await _context.Bookings
                .Include(b => b.ServiceItem)
                .Include(b => b.Customer).ThenInclude(c => c.User)
                .Include(b => b.ServiceProvider).ThenInclude(p => p.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (booking == null) return NotFound();

            var userId = _userManager.GetUserId(User);
            var isProvider = User.IsInRole("ServiceProvider");

            // Authorization Check
            if (isProvider)
            {
                if (booking.ServiceProvider.UserId != userId) return Forbid();
                ViewBag.IsProvider = true;
            }
            else
            {
                if (booking.Customer.UserId != userId) return Forbid();
                ViewBag.IsProvider = false;
            }

            return View(booking);
        }
    }

    }

