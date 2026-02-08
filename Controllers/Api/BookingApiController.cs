using FixItNepal.Data;
using FixItNepal.Models;
using FixItNepal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FixItNepal.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "Bearer,Identity.Application")]
    public class BookingApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;

        public BookingApiController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IEmailService emailService)
        {
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
        }

        [HttpGet("slots/{providerId}/{date}")]
        public async Task<IActionResult> GetBookedSlots(int providerId, DateTime date)
        {
            var bookings = await _context.Bookings
                .Where(b => b.ServiceProviderId == providerId && b.BookingDate.Date == date.Date && b.Status != BookingStatus.Cancelled && b.Status != BookingStatus.Rejected)
                .Select(b => new { start = b.StartTime, end = b.EndTime })
                .ToListAsync();

            var availability = await _context.ProviderAvailabilities
                .FirstOrDefaultAsync(a => a.ServiceProviderId == providerId && a.DayOfWeek == date.DayOfWeek);

            if (availability == null)
            {
                // Default Availability: 10 AM to 5 PM
                availability = new ProviderAvailability
                {
                    ServiceProviderId = providerId,
                    DayOfWeek = date.DayOfWeek,
                    StartTime = new TimeSpan(10, 0, 0),
                    EndTime = new TimeSpan(17, 0, 0),
                    IsDayOff = false
                };
            }

            return Ok(new { bookings, availability });
        }

        // GET: api/BookingApi
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Booking>>> GetBookings()
        {
            var userId = _userManager.GetUserId(User);
            var isProvider = User.IsInRole("ServiceProvider");

            if (isProvider)
            {
                 var bookings = await _context.Bookings
                    .Include(b => b.Customer)
                    .ThenInclude(c => c.User)
                    .Include(b => b.ServiceItem)
                    .Where(b => b.ServiceProvider.UserId == userId)
                    .OrderByDescending(b => b.BookingDate)
                    .ToListAsync();
                 return Ok(bookings);
            }
            else
            {
                var bookings = await _context.Bookings
                    .Include(b => b.ServiceProvider)
                    .ThenInclude(p => p.User)
                    .Include(b => b.ServiceItem)
                    .Where(b => b.Customer.UserId == userId)
                    .OrderByDescending(b => b.BookingDate)
                    .ToListAsync();
                return Ok(bookings);
            }
        }

        // GET: api/BookingApi/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Booking>> GetBooking(int id)
        {
            var userId = _userManager.GetUserId(User);
            var booking = await _context.Bookings
                 .Include(b => b.ServiceProvider)
                 .Include(b => b.Customer)
                 .Include(b => b.ServiceItem)
                 .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null) return NotFound();

            // Authorization check
            if (booking.Customer.UserId != userId && booking.ServiceProvider.UserId != userId)
            {
                return Forbid();
            }

            return Ok(booking);
        }

        // POST: api/BookingApi
        [HttpPost]
        public async Task<ActionResult<Booking>> CreateBooking(BookingDto bookingDto)
        {
            var userId = _userManager.GetUserId(User);
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == userId);

            if (customer == null)
            {
                return BadRequest("User is not a registered customer.");
            }

            // prevent booking if provider is not available or conflict exists
            // 1. Check if provider exists
            var serviceProvider = await _context.ServiceProviders.FindAsync(bookingDto.ServiceProviderId);
            if (serviceProvider == null) return NotFound("Provider not found.");

            // 2. Check overlap
            // Use range for date comparison to be safe against time components
            var checkDate = bookingDto.BookingDate.Date;
            var nextDate = checkDate.AddDays(1);

            Console.WriteLine($"[DEBUG] Checking Conflict for Provider {bookingDto.ServiceProviderId} on {checkDate}");

            var conflicts = await _context.Bookings.Where(b =>
                b.ServiceProviderId == bookingDto.ServiceProviderId &&
                b.BookingDate >= checkDate && b.BookingDate < nextDate &&
                b.Status != BookingStatus.Cancelled &&
                b.Status != BookingStatus.Rejected).ToListAsync();

            Console.WriteLine($"[DEBUG] Found {conflicts.Count} bookings on this date.");
            
            var hasConflict = conflicts.Any(b => (b.StartTime < bookingDto.EndTime) && (b.EndTime > bookingDto.StartTime));

            if (hasConflict)
            {
                Console.WriteLine("[DEBUG] CONFLICT DETECTED!");
                return Conflict("The selected time slot is already booked.");
            }
            else 
            {
                 Console.WriteLine("[DEBUG] No Conflict.");
            }
            
             // 3. Check Provider Availability (if set)
            var availability = await _context.ProviderAvailabilities
                .FirstOrDefaultAsync(a => a.ServiceProviderId == bookingDto.ServiceProviderId && a.DayOfWeek == bookingDto.BookingDate.DayOfWeek);

            // If availability is null, assume default 10-5
            if (availability == null)
            {
                 availability = new ProviderAvailability
                 {
                     StartTime = new TimeSpan(10, 0, 0),
                     EndTime = new TimeSpan(17, 0, 0),
                     IsDayOff = false
                 };
            }

            if (availability != null)
            {
                if (availability.IsDayOff)
                {
                    return BadRequest("Provider is not available on this day.");
                }
                if (bookingDto.StartTime < availability.StartTime || bookingDto.EndTime > availability.EndTime)
                {
                    return BadRequest($"Provider is only available between {availability.StartTime} and {availability.EndTime}.");
                }
            }


            var serviceItem = await _context.ServiceItems.FindAsync(bookingDto.ServiceItemId);
            if (serviceItem == null) return NotFound("Service not found.");

            var booking = new Booking
            {
                CustomerId = customer.Id,
                ServiceProviderId = bookingDto.ServiceProviderId,
                ServiceItemId = bookingDto.ServiceItemId,
                BookingDate = bookingDto.BookingDate,
                StartTime = bookingDto.StartTime,
                EndTime = bookingDto.EndTime,
                Status = BookingStatus.Pending,
                TotalPrice = serviceItem.BasePrice, // Simple pricing for now
                Notes = bookingDto.Notes,
                CustomerAddress = bookingDto.CustomerAddress,
                CustomerPhone = bookingDto.CustomerPhone
            };

            _context.Bookings.Add(booking);
            
            // Create Notification for Provider
            var notification = new Notification
            {
                UserId = serviceProvider.UserId,
                Title = "New Booking Request",
                Message = $"You have a new booking request for {serviceItem.Name} on {booking.BookingDate.ToShortDateString()}.",
                Type = NotificationType.System,
                RelatedEntityId = booking.Id,
                RelatedEntityType = "Booking"
            };
            _context.Notifications.Add(notification);

            await _context.SaveChangesAsync();

            // Send Email to Provider
            var providerUser = await _userManager.FindByIdAsync(serviceProvider.UserId);
            if (providerUser != null)
            {
                var subject = "New Booking Request - FixIt Nepal";
                var body = $@"
                    <h2>New Booking Request</h2>
                    <p>Hello {providerUser.FullName},</p>
                    <p>You have received a new booking request for <strong>{serviceItem.Name}</strong>.</p>
                    <p><strong>Date:</strong> {booking.BookingDate.ToShortDateString()}</p>
                    <p><strong>Time:</strong> {booking.StartTime} - {booking.EndTime}</p>
                    <p>Please login to your dashboard to review and accept the booking.</p>
                ";
                await _emailService.SendEmailAsync(providerUser.Email, subject, body);
            }

            return CreatedAtAction("GetBookings", new { id = booking.Id }, booking);
        }

        // PUT: api/BookingApi/5/status
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusDto statusDto)
        {
             var userId = _userManager.GetUserId(User);
             var booking = await _context.Bookings
                 .Include(b => b.ServiceProvider)
                 .Include(b => b.Customer)
                 .FirstOrDefaultAsync(b => b.Id == id);

             if (booking == null) return NotFound();

             // Only provider can accept/reject/complete
             // Customer can cancel
             
             if (booking.ServiceProvider.UserId == userId)
             {
                 if (statusDto.Status == BookingStatus.Confirmed || statusDto.Status == BookingStatus.Rejected || statusDto.Status == BookingStatus.Completed)
                 {
                     booking.Status = statusDto.Status;
                     
                     // Notify Customer
                     _context.Notifications.Add(new Notification
                     {
                         UserId = booking.Customer.UserId,
                         Title = $"Booking {statusDto.Status}",
                         Message = $"Your booking for {booking.BookingDate.ToShortDateString()} has been {statusDto.Status}.",
                         RelatedEntityId = booking.Id,
                         RelatedEntityType = "Booking"
                     });
                 }
                 else
                 {
                     return Forbid();
                 }
             }
             else if (booking.Customer.UserId == userId)
             {
                 if (statusDto.Status == BookingStatus.Cancelled)
                 {
                     booking.Status = statusDto.Status;
                     
                     // Notify Provider
                     _context.Notifications.Add(new Notification
                     {
                         UserId = booking.ServiceProvider.UserId,
                         Title = "Booking Cancelled",
                         Message = $"Booking #{booking.Id} has been cancelled by the customer.",
                         RelatedEntityId = booking.Id,
                         RelatedEntityType = "Booking"
                     });
                 }
                 else
                 {
                     return Forbid();
                 }
             }
             else
             {
                 return Forbid();
             }

              await _context.SaveChangesAsync();

              // Send Email to Customer
              var customerUser = await _userManager.FindByIdAsync(booking.Customer.UserId);
              if (customerUser != null)
              {
                  var subject = $"Booking Updated - {booking.Status}";
                  var body = $@"
                      <h2>Booking Update</h2>
                      <p>Hello {customerUser.FullName},</p>
                      <p>Your booking for <strong>{booking.BookingDate.ToShortDateString()}</strong> has been <strong>{booking.Status}</strong>.</p>
                      <p>Thank you for using FixIt Nepal.</p>
                  ";
                  await _emailService.SendEmailAsync(customerUser.Email, subject, body);
              }

              return Ok(new { message = "Booking status updated successfully", newStatus = booking.Status.ToString() });
        }
    }

    public class BookingDto
    {
        public int ServiceProviderId { get; set; }
        public int ServiceItemId { get; set; }
        public DateTime BookingDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string? Notes { get; set; }
        public string? CustomerAddress { get; set; }
        public string? CustomerPhone { get; set; }
    }

    public class UpdateStatusDto
    {
        public BookingStatus Status { get; set; }
    }
}
