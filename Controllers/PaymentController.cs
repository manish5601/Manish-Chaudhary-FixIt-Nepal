using FixItNepal.Data;
using FixItNepal.Models;
using FixItNepal.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace FixItNepal.Controllers
{
    public class PaymentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IESewaService _eSewaService;
        private readonly IEmailService _emailService;
        private readonly UserManager<ApplicationUser> _userManager;

        public PaymentController(
            ApplicationDbContext context, 
            IESewaService eSewaService, 
            IEmailService emailService,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _eSewaService = eSewaService;
            _emailService = emailService;
            _userManager = userManager;
        }

        // GET: /Payment/Success?data=...
        public async Task<IActionResult> Success(string data)
        {
            if (string.IsNullOrEmpty(data)) return BadRequest();

            bool isValid = await _eSewaService.VerifyPaymentAsync(data);
            if (!isValid)
            {
                TempData["ErrorMessage"] = "Payment verification failed. Please contact support.";
                return RedirectToAction("MyBookings", "Booking");
            }

            // Decode data to get transaction_uuid which contains our Booking ID
            byte[] decodedBytes = Convert.FromBase64String(data);
            string jsonString = System.Text.Encoding.UTF8.GetString(decodedBytes);
            
            // Simplified JSON parsing for example
            var dynamicData = System.Text.Json.JsonDocument.Parse(jsonString).RootElement;
            string bookingIdStr = dynamicData.GetProperty("transaction_uuid").GetString() ?? "";
            string transactionCode = dynamicData.GetProperty("transaction_code").GetString() ?? "";

            if (int.TryParse(bookingIdStr.Split('-')[0], out int bookingId))
            {
                var booking = await _context.Bookings
                    .Include(b => b.Customer).ThenInclude(c => c.User)
                    .Include(b => b.ServiceProvider).ThenInclude(p => p.User)
                    .Include(b => b.ServiceItem)
                    .FirstOrDefaultAsync(b => b.Id == bookingId);

                if (booking != null && booking.Status == BookingStatus.PaymentPending)
                {
                    booking.Status = BookingStatus.Pending; // Now it's a real booking for provider to see
                    booking.IsTokenPaid = true;
                    booking.ESewaTransactionId = transactionCode;
                    booking.PaidAt = DateTime.UtcNow;
                    booking.ExpiresAt = DateTime.UtcNow.AddHours(24); // Start the 24h timer now

                    _context.Update(booking);
                    
                    // --- Notification logic moved here from BookingController ---
                    var providerUser = booking.ServiceProvider.User;
                    if (providerUser != null)
                    {
                        var notif = new Notification
                        {
                            UserId = providerUser.Id,
                            Title = "New Booking Order - Paid",
                            Message = $"New booking for {booking.ServiceItem.Name}. Token paid. Reply within 24 hours.",
                            Type = NotificationType.System,
                            IsRead = false,
                            CreatedAt = DateTime.UtcNow,
                            RelatedEntityId = booking.Id,
                            RelatedEntityType = "Booking"
                        };
                        _context.Notifications.Add(notif);

                        // Send Email to Provider
                        var subject = $"New Booking Request: {booking.ServiceItem.Name} - FixIt Nepal";
                        var body = $@"
                            <div style='font-family: sans-serif; color: #333;'>
                                <h2 style='color: #0d6efd;'>New Paid Booking Request</h2>
                                <p>Hello <strong>{providerUser.FullName}</strong>,</p>
                                <p>You have received a new booking request. The customer has paid the Rs 10 token amount.</p>
                                <p><strong>IMPORTANT:</strong> You must Accept or Reject this booking within <strong>24 hours</strong> or it will expire.</p>
                                <div style='background-color: #f8f9fa; padding: 15px; border-radius: 8px; margin: 20px 0;'>
                                    <h4 style='margin-top: 0;'>Booking Details:</h4>
                                    <ul style='list-style: none; padding: 0;'>
                                        <li><strong>Date:</strong> {booking.BookingDate.ToShortDateString()}</li>
                                        <li><strong>Time Slot:</strong> {booking.StartTime} - {booking.EndTime}</li>
                                        <li><strong>Total Price:</strong> Rs. {booking.TotalPrice}</li>
                                    </ul>
                                </div>
                                <p><a href='https://fixitnepal.com/Booking/Details/{booking.Id}'>View Booking Details</a></p>
                            </div>";
                        await _emailService.SendEmailAsync(providerUser.Email, subject, body);
                    }

                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Payment successful! Your booking request has been sent to the provider.";
                }
            }

            return RedirectToAction("MyBookings", "Booking");
        }

        // GET: /Payment/Failure
        public IActionResult Failure()
        {
            TempData["ErrorMessage"] = "Payment failed. Please try again.";
            return RedirectToAction("MyBookings", "Booking");
        }
    }
}
