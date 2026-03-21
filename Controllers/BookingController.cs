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
        private readonly IESewaService _eSewaService;
        private readonly ESewaSettings _eSewaSettings;

        public BookingController(
            ApplicationDbContext context, 
            UserManager<ApplicationUser> userManager, 
            IEmailService emailService,
            IESewaService eSewaService,
            Microsoft.Extensions.Options.IOptions<ESewaSettings> eSewaSettings)
        {
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
            _eSewaService = eSewaService;
            _eSewaSettings = eSewaSettings.Value;
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
                var customer = await _context.Customers.Include(c => c.User).FirstOrDefaultAsync(c => c.UserId == userId);

                if (customer == null)
                {
                    return RedirectToAction("RegisterCustomer", "Auth"); 
                }

                // Call API logic here or reuse service? 
                // For simplified architecture, we'll duplicate simplified logic or call the internal service if we had one.
                // Since verified API exists, let's use the Context directly but ideally we should use the API/Service.
                // Keeping it direct for now as per previous pattern.

                var bookingInProgress = new Booking
                {
                    CustomerId = customer.Id,
                    ServiceProviderId = model.ServiceProviderId,
                    ServiceItemId = model.ServiceItemId,
                    BookingDate = model.BookingDate,
                    StartTime = model.StartTime,
                    EndTime = model.EndTime,
                    TotalPrice = model.Price,
                    CustomerAddress = model.CustomerAddress,
                    CustomerPhone = model.CustomerPhone,
                    Notes = model.Notes,
                    Status = BookingStatus.PaymentPending, // Wait for payment
                    TokenAmount = 10.00m,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Bookings.Add(bookingInProgress);
                await _context.SaveChangesAsync();

                // Prepare eSewa payment
                var transactionUuid = $"{bookingInProgress.Id}-{DateTime.UtcNow.Ticks}";
                var signature = _eSewaService.GenerateSignature(10.00m, transactionUuid, _eSewaSettings.ProductCode);

                var paymentModel = new ESewaPaymentViewModel
                {
                    Amount = "10",
                    TotalAmount = "10",
                    TransactionUuid = transactionUuid,
                    ProductCode = _eSewaSettings.ProductCode,
                    Signature = signature,
                    BaseUrl = _eSewaSettings.BaseUrl,
                    SuccessUrl = $"{Request.Scheme}://{Request.Host}/Payment/Success",
                    FailureUrl = $"{Request.Scheme}://{Request.Host}/Payment/Failure"
                };

                return View("~/Views/Payment/RedirectToESewa.cshtml", paymentModel);
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

                // Check for Expired Bookings
                var pendingBookingsList = await _context.Bookings
                    .Where(b => b.ServiceProviderId == provider.Id && b.Status == BookingStatus.Pending && b.ExpiresAt < DateTime.UtcNow)
                    .ToListAsync();
                
                if (pendingBookingsList.Any())
                {
                    foreach(var b in pendingBookingsList) 
                        b.Status = b.IsTokenPaid ? BookingStatus.RefundPending : BookingStatus.Expired;
                    await _context.SaveChangesAsync();
                }

                var bookings = await _context.Bookings
                    .Include(b => b.Customer).ThenInclude(c => c.User)
                    .Include(b => b.ServiceItem)
                    .Where(b => b.ServiceProviderId == provider.Id && b.Status != BookingStatus.PaymentPending) // Hide unpaid bookings from provider
                    .OrderByDescending(b => b.BookingDate)
                    .ToListAsync();
                 
                 ViewBag.IsProvider = true;
                 return View(bookings);
            }
            else
            {
                var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == userId);
                if (customer == null) return View(new List<Booking>());

                // Check for Expired Bookings (Customer side cleanup)
                var customerPendingBookings = await _context.Bookings
                    .Where(b => b.CustomerId == customer.Id && b.Status == BookingStatus.Pending && b.ExpiresAt < DateTime.UtcNow)
                    .ToListAsync();

                if (customerPendingBookings.Any())
                {
                    foreach (var b in customerPendingBookings) 
                        b.Status = b.IsTokenPaid ? BookingStatus.RefundPending : BookingStatus.Expired;
                    await _context.SaveChangesAsync();
                }

                var bookings = await _context.Bookings
                    .Include(b => b.ServiceProvider).ThenInclude(p => p.User)
                    .Include(b => b.ServiceItem)
                    .Where(b => b.CustomerId == customer.Id)
                    .OrderByDescending(b => b.CreatedAt)
                    .ToListAsync();

                 ViewBag.IsProvider = false;
                 return View(bookings);
            }
        }

        // GET: Booking/PayNow/5
        public async Task<IActionResult> PayNow(int id)
        {
            var userId = _userManager.GetUserId(User);
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == userId);
            if (customer == null) return Unauthorized();

            var booking = await _context.Bookings
                .FirstOrDefaultAsync(b => b.Id == id && b.CustomerId == customer.Id && b.Status == BookingStatus.PaymentPending);

            if (booking == null) return NotFound("Booking not found or not in payment pending state.");

            // Prepare eSewa payment (same logic as in Create)
            var transactionUuid = $"{booking.Id}-{DateTime.UtcNow.Ticks}";
            var signature = _eSewaService.GenerateSignature(10.00m, transactionUuid, _eSewaSettings.ProductCode);

            var paymentModel = new ESewaPaymentViewModel
            {
                Amount = "10",
                TotalAmount = "10",
                TransactionUuid = transactionUuid,
                ProductCode = _eSewaSettings.ProductCode,
                Signature = signature,
                BaseUrl = _eSewaSettings.BaseUrl,
                SuccessUrl = $"{Request.Scheme}://{Request.Host}/Payment/Success",
                FailureUrl = $"{Request.Scheme}://{Request.Host}/Payment/Failure"
            };

            return View("~/Views/Payment/RedirectToESewa.cshtml", paymentModel);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int id, BookingStatus status)
        {
            var booking = await _context.Bookings.Include(b => b.Customer).Include(b => b.ServiceProvider).FirstOrDefaultAsync(b => b.Id == id);
            if (booking == null) return NotFound();

            var userId = _userManager.GetUserId(User);
            var isProvider = User.IsInRole("ServiceProvider");

            // --- STRICT CANCELLATION RULES ---
            if (!isProvider && status == BookingStatus.Cancelled)
            {
                if (booking.Status == BookingStatus.Confirmed || booking.Status == BookingStatus.Completed || booking.Status == BookingStatus.AwaitingConfirmation)
                {
                    TempData["ErrorMessage"] = "You cannot cancel a booking once it has been confirmed or is in progress. Please contact the provider or support.";
                    return RedirectToAction("Details", new { id = booking.Id });
                }
            }

            // --- COMPLETION FLOW RULES ---
            // Only customer can mark as "Completed" from "AwaitingConfirmation"
            if (!isProvider && status == BookingStatus.Completed)
            {
                if (booking.Status != BookingStatus.AwaitingConfirmation)
                {
                    TempData["ErrorMessage"] = "You can only confirm completion for jobs awaiting your confirmation.";
                    return RedirectToAction("Details", new { id = booking.Id });
                }
            }
            // Only provider can mark as "AwaitingConfirmation" from "Confirmed"
            if (isProvider && status == BookingStatus.AwaitingConfirmation)
            {
                if (booking.Status != BookingStatus.Confirmed)
                {
                    TempData["ErrorMessage"] = "You can only mark confirmed jobs as awaiting confirmation.";
                    return RedirectToAction("Details", new { id = booking.Id });
                }
            }
            
            if (isProvider && status == BookingStatus.Rejected && booking.IsTokenPaid)
            {
                booking.Status = BookingStatus.RefundPending;
            }
            else
            {
                booking.Status = status;
            }
            
            // Notify other party
            string targetUserId = "";
            string message = "";
            
            if (isProvider)
            {
                targetUserId = booking.Customer.UserId;
                if (status == BookingStatus.AwaitingConfirmation)
                    message = $"The provider has marked the service for {booking.ServiceItem?.Name} as finished. Please confirm the completion.";
                else
                    message = $"Your booking for {booking.ServiceItem?.Name} has been {status}.";
            }
            else
            {
                targetUserId = booking.ServiceProvider.UserId;
                if (status == BookingStatus.Completed)
                    message = $"Customer has confirmed completion for {booking.ServiceItem?.Name}. You can now view any feedback received.";
                else
                    message = $"Booking for {booking.ServiceItem?.Name} has been {status} by customer.";
            }

            var notif = new Notification
            {
                UserId = targetUserId,
                Title = $"Booking {status}",
                Message = message,
                Type = NotificationType.System,
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
                RelatedEntityId = booking.Id,
                RelatedEntityType = "Booking"
            };
            _context.Notifications.Add(notif);

            // Send Email Notification
            var targetUser = await _userManager.FindByIdAsync(targetUserId);
            if (targetUser != null)
            {
                 var subject = $"Booking Update: {status} - FixIt Nepal";
                 var body = $@"
                    <div style='font-family: sans-serif; color: #333;'>
                        <h2 style='color: #0d6efd;'>Booking Update</h2>
                        <p>Hello <strong>{targetUser.FullName}</strong>,</p>
                        <p style='font-size: 1.1em;'>{message}</p>
                        <div style='background-color: #f8f9fa; padding: 15px; border-radius: 8px; margin: 20px 0;'>
                            <h4 style='margin-top: 0;'>Booking Summary:</h4>
                            <ul style='list-style: none; padding: 0;'>
                                <li><strong>Service:</strong> {booking.ServiceItem?.Name}</li>
                                <li><strong>Date:</strong> {booking.BookingDate.ToShortDateString()}</li>
                                <li><strong>Time Slot:</strong> {booking.StartTime} - {booking.EndTime}</li>
                                <li><strong>Status:</strong> <span style='font-weight: bold;'>{status}</span></li>
                            </ul>
                        </div>
                        <p>You can view more details on your <a href='https://fixitnepal.com/Booking/Details/{booking.Id}'>Booking Details</a> page.</p>
                        <hr style='border: 0; border-top: 1px solid #eee; margin: 20px 0;'>
                        <p style='font-size: 0.9em; color: #666;'>Thank you for using FixIt Nepal.</p>
                    </div>
                 ";
                 await _emailService.SendEmailAsync(targetUser.Email, subject, body);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Details", new { id = booking.Id });
        }

        // GET: Booking/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var booking = await _context.Bookings
                .Include(b => b.ServiceItem)
                .Include(b => b.Customer).ThenInclude(c => c.User)
                .Include(b => b.ServiceProvider).ThenInclude(p => p.User)
                .Include(b => b.Review)
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

