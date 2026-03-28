using FixItNepal.Data;
using FixItNepal.Models;
using FixItNepal.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FixItNepal.Controllers
{
    [Authorize]
    public class ServiceRequestController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public ServiceRequestController(UserManager<ApplicationUser> userManager, ApplicationDbContext context, IWebHostEnvironment env)
        {
            _userManager = userManager;
            _context = context;
            _env = env;
        }

        // --- CUSTOMER ACTIONS ---

        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Create()
        {
            var categories = await _context.ServiceCategories.Where(c => c.IsActive).ToListAsync();
            var model = new ServiceRequestViewModel
            {
                Categories = categories.Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name })
            };
            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Customer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ServiceRequestViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == user.Id);
            if (customer == null) return NotFound("Customer profile not found.");

            // Remove ImageFile from validation (it's optional)
            ModelState.Remove(nameof(model.ImageFile));

            if (ModelState.IsValid)
            {
                string? imageUrl = null;
                if (model.ImageFile != null && model.ImageFile.Length > 0)
                {
                    var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "requests");
                    Directory.CreateDirectory(uploadsFolder);
                    var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(model.ImageFile.FileName)}";
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                        await model.ImageFile.CopyToAsync(stream);
                    imageUrl = uniqueFileName;
                }

                var request = new ServiceRequest
                {
                    CustomerId = customer.Id,
                    ServiceCategoryId = model.ServiceCategoryId,
                    Title = model.Title,
                    Description = model.Description,
                    Address = model.Address,
                    Latitude = model.Latitude,
                    Longitude = model.Longitude,
                    ImageUrl = imageUrl,
                    Status = ServiceRequestStatus.Open
                };

                _context.ServiceRequests.Add(request);
                await _context.SaveChangesAsync();

                // Notify all providers in the same category, EXCLUDING the current customer if they are also a provider
                var categoryProviders = await _context.ServiceProviders
                    .Where(p => p.ServiceCategoryId == model.ServiceCategoryId 
                           && p.Status == VerificationStatus.Approved 
                           && p.UserId != user.Id)
                    .Select(p => p.UserId)
                    .ToListAsync();

                foreach (var providerUserId in categoryProviders)
                {
                    _context.Notifications.Add(new Notification
                    {
                        UserId = providerUserId,
                        Title = "New Job Available!",
                        Message = $"A new job has been posted in your category: \"{request.Title}\". Be the first to bid!",
                        Type = NotificationType.System,
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow,
                        RelatedEntityId = request.Id,
                        RelatedEntityType = "ServiceRequest"
                    });
                }
                if (categoryProviders.Any())
                    await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Your request has been posted! Providers in this category will be notified.";
                return RedirectToAction(nameof(MyRequests));
            }

            var categories = await _context.ServiceCategories.Where(c => c.IsActive).ToListAsync();
            model.Categories = categories.Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name });
            return View(model);
        }

        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Edit(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == user.Id);
            
            var request = await _context.ServiceRequests.Include(r => r.Bids)
                .FirstOrDefaultAsync(r => r.Id == id && r.CustomerId == customer.Id);
            
            if (request == null) return NotFound();
            if (request.Bids.Any())
            {
                TempData["ErrorMessage"] = "You cannot edit a request that already has bids.";
                return RedirectToAction(nameof(Details), new { id = request.Id });
            }

            var categories = await _context.ServiceCategories.Where(c => c.IsActive).ToListAsync();
            var model = new ServiceRequestViewModel
            {
                Title = request.Title,
                Description = request.Description,
                ServiceCategoryId = request.ServiceCategoryId,
                Address = request.Address,
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                ExistingImageUrl = request.ImageUrl,
                Categories = categories.Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name })
            };

            ViewBag.RequestId = id;
            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Customer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ServiceRequestViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == user.Id);
            
            var request = await _context.ServiceRequests.Include(r => r.Bids)
                .FirstOrDefaultAsync(r => r.Id == id && r.CustomerId == customer.Id);
            
            if (request == null) return NotFound();
            if (request.Bids.Any()) return Forbid(); // Should not happen via UI

            ModelState.Remove(nameof(model.ImageFile));

            if (ModelState.IsValid)
            {
                request.Title = model.Title;
                request.Description = model.Description;
                request.ServiceCategoryId = model.ServiceCategoryId;
                request.Address = model.Address;
                request.Latitude = model.Latitude;
                request.Longitude = model.Longitude;

                // Handle image upload (replace or keep existing)
                if (model.ImageFile != null && model.ImageFile.Length > 0)
                {
                    var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "requests");
                    Directory.CreateDirectory(uploadsFolder);
                    var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(model.ImageFile.FileName)}";
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                        await model.ImageFile.CopyToAsync(stream);
                    request.ImageUrl = uniqueFileName;
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Your request has been updated successfully.";
                return RedirectToAction(nameof(Details), new { id = request.Id });
            }

            var categories = await _context.ServiceCategories.Where(c => c.IsActive).ToListAsync();
            model.Categories = categories.Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name });
            model.ExistingImageUrl = request.ImageUrl;
            ViewBag.RequestId = id;
            return View(model);
        }

        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> MyRequests()
        {
            var user = await _userManager.GetUserAsync(User);
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == user.Id);
            if (customer == null) return NotFound();

            var requests = await _context.ServiceRequests
                .Include(r => r.ServiceCategory)
                .Include(r => r.Bids)
                    .ThenInclude(b => b.ServiceProvider)
                        .ThenInclude(p => p.User)
                .Where(r => r.CustomerId == customer.Id)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return View(requests);
        }

        // --- PROVIDER ACTIONS ---

        [Authorize(Roles = "ServiceProvider")]
        public async Task<IActionResult> Marketplace()
        {
            var user = await _userManager.GetUserAsync(User);
            var provider = await _context.ServiceProviders.FirstOrDefaultAsync(p => p.UserId == user.Id);
            if (provider == null) return NotFound("Provider profile not found.");

            // Show Open or Bidding requests in the provider's category
            var requests = await _context.ServiceRequests
                .Include(r => r.Customer)
                    .ThenInclude(c => c.User)
                .Include(r => r.ServiceCategory)
                .Include(r => r.Bids)
                .Where(r => r.ServiceCategoryId == provider.ServiceCategoryId && (r.Status == ServiceRequestStatus.Open || r.Status == ServiceRequestStatus.Bidding))
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
            
            ViewBag.ProviderUserId = user.Id;
            return View(requests);
        }

        [HttpPost]
        [Authorize(Roles = "ServiceProvider")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitBid(ServiceBidViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            var provider = await _context.ServiceProviders.FirstOrDefaultAsync(p => p.UserId == user.Id);
            if (provider == null) return NotFound();

            if (ModelState.IsValid)
            {
                // Check if already bid
                var existingBid = await _context.ServiceBids
                    .FirstOrDefaultAsync(b => b.ServiceRequestId == model.ServiceRequestId && b.ServiceProviderId == provider.Id);
                
                if (existingBid != null)
                {
                    TempData["ErrorMessage"] = "You have already submitted a bid for this request.";
                    return RedirectToAction(nameof(Details), new { id = model.ServiceRequestId });
                }

                var bid = new ServiceBid
                {
                    ServiceRequestId = model.ServiceRequestId,
                    ServiceProviderId = provider.Id,
                    ProposedPrice = model.ProposedPrice,
                    EstimatedTime = model.EstimatedTime,
                    Notes = model.Notes,
                    Status = BidStatus.Pending
                };

                _context.ServiceBids.Add(bid);
                
                // Update request status to Bidding if it's still Open
                var request = await _context.ServiceRequests.FindAsync(model.ServiceRequestId);
                if (request != null && request.Status == ServiceRequestStatus.Open)
                {
                    request.Status = ServiceRequestStatus.Bidding;
                }

                await _context.SaveChangesAsync();

                // Notify Customer
                var customerUserId = (await _context.Customers.FirstOrDefaultAsync(c => c.Id == request.CustomerId))?.UserId;
                if (customerUserId != null)
                {
                    var notif = new Notification
                    {
                        UserId = customerUserId,
                        Title = "New Bid Received",
                        Message = $"Provider {provider.User?.FullName ?? "Someone"} has placed a bid of Rs. {model.ProposedPrice} on your request: {request.Title}",
                        Type = NotificationType.System,
                        CreatedAt = DateTime.UtcNow,
                        RelatedEntityId = request.Id,
                        RelatedEntityType = "ServiceRequest"
                    };
                    _context.Notifications.Add(notif);
                    await _context.SaveChangesAsync();
                }

                TempData["SuccessMessage"] = "Your bid has been submitted!";
                return RedirectToAction(nameof(Details), new { id = model.ServiceRequestId });
            }
            
            TempData["ErrorMessage"] = "Invalid bid data. Please try again.";
            return RedirectToAction(nameof(Details), new { id = model.ServiceRequestId });
        }

        [HttpPost]
        [Authorize(Roles = "ServiceProvider")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateBid(int bidId, decimal ProposedPrice, string EstimatedTime, string Notes)
        {
            var user = await _userManager.GetUserAsync(User);
            var provider = await _context.ServiceProviders.FirstOrDefaultAsync(p => p.UserId == user.Id);
            if (provider == null) return NotFound();

            var bid = await _context.ServiceBids.FirstOrDefaultAsync(b => b.Id == bidId && b.ServiceProviderId == provider.Id);
            if (bid == null || bid.Status == BidStatus.Accepted) return NotFound("Cannot edit this bid.");

            // Update Bid
            bid.ProposedPrice = ProposedPrice;
            bid.EstimatedTime = EstimatedTime;
            bid.Notes = Notes;
            
            // Clear any counter-offer since the provider has given a new offer
            bid.CustomerCounterPrice = null;
            bid.CustomerCounterMessage = null;
            bid.Status = BidStatus.Pending;

            await _context.SaveChangesAsync();

            // Notify Customer
            var request = await _context.ServiceRequests.Include(r => r.Customer).FirstOrDefaultAsync(r => r.Id == bid.ServiceRequestId);
            if (request != null)
            {
                var notif = new Notification
                {
                    UserId = request.Customer.UserId,
                    Title = "Bid Updated",
                    Message = $"Provider {provider.User?.FullName ?? "A provider"} has updated their bid on your request to Rs. {ProposedPrice}",
                    Type = NotificationType.System,
                    CreatedAt = DateTime.UtcNow,
                    RelatedEntityId = request.Id,
                    RelatedEntityType = "ServiceRequest"
                };
                _context.Notifications.Add(notif);
                await _context.SaveChangesAsync();
            }

            TempData["SuccessMessage"] = "Your bid has been updated!";
            return RedirectToAction(nameof(Details), new { id = bid.ServiceRequestId });
        }

        // --- SHARED ACTIONS ---

        public async Task<IActionResult> Details(int id)
        {
            var request = await _context.ServiceRequests
                .Include(r => r.Customer).ThenInclude(c => c.User)
                .Include(r => r.ServiceCategory)
                .Include(r => r.Bids).ThenInclude(b => b.ServiceProvider).ThenInclude(p => p.User)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            ViewBag.IsCustomer = await _userManager.IsInRoleAsync(user, "Customer");
            ViewBag.IsProvider = await _userManager.IsInRoleAsync(user, "ServiceProvider");
            
            // For providers, check if they've already bid
            if (ViewBag.IsProvider)
            {
                var provider = await _context.ServiceProviders.FirstOrDefaultAsync(p => p.UserId == user.Id);
                ViewBag.HasAlreadyBid = request.Bids.Any(b => b.ServiceProviderId == provider?.Id);
            }

            return View(request);
        }

        [HttpPost]
        [Authorize(Roles = "Customer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptBid(int bidId)
        {
            var bid = await _context.ServiceBids
                .Include(b => b.ServiceRequest)
                .Include(b => b.ServiceProvider).ThenInclude(p => p.User)
                .FirstOrDefaultAsync(b => b.Id == bidId);

            if (bid == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == user.Id);
            if (customer == null || bid.ServiceRequest.CustomerId != customer.Id) return Forbid();

            // Set bid and request status
            bid.Status = BidStatus.Accepted;
            bid.ServiceRequest.Status = ServiceRequestStatus.Accepted;

            // Reject all other pending bids for this request
            var otherBids = await _context.ServiceBids
                .Where(b => b.ServiceRequestId == bid.ServiceRequestId && b.Id != bidId && b.Status == BidStatus.Pending)
                .ToListAsync();
            foreach (var b in otherBids) b.Status = BidStatus.Rejected;

            // Create a Booking (PaymentPending)
            var booking = new Booking
            {
                CustomerId = bid.ServiceRequest.CustomerId,
                ServiceProviderId = bid.ServiceProviderId,
                ServiceRequestId = bid.ServiceRequestId,
                BookingDate = DateTime.Now.Date.AddDays(1), // Default to tomorrow
                StartTime = new TimeSpan(9, 0, 0), // Default 9 AM
                EndTime = new TimeSpan(11, 0, 0),
                Status = BookingStatus.PaymentPending,
                TotalPrice = bid.ProposedPrice,
                TokenAmount = Math.Max(10, bid.ProposedPrice * 0.1m), // 10% or min 10 NPR
                CustomerAddress = bid.ServiceRequest.Address,
                Notes = $"On-Demand Request: {bid.ServiceRequest.Title}\nBargained Price: Rs {bid.ProposedPrice}\nProvider Notes: {bid.Notes}"
            };

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Bid accepted! Please complete the token payment to confirm your booking.";
            
            // Redirect to Payment
            return RedirectToAction("PayNow", "Booking", new { id = booking.Id });
        }

        [HttpPost]
        [Authorize(Roles = "Customer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CounterBid(int bidId, decimal CounterPrice, string? CounterMessage)
        {
            var user = await _userManager.GetUserAsync(User);
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == user.Id);
            
            var bid = await _context.ServiceBids.Include(b => b.ServiceRequest)
                .FirstOrDefaultAsync(b => b.Id == bidId && b.ServiceRequest.CustomerId == customer.Id);

            if (bid == null || bid.Status == BidStatus.Accepted || bid.Status == BidStatus.Rejected) 
                return NotFound("Cannot counter this bid.");

            bid.CustomerCounterPrice = CounterPrice;
            bid.CustomerCounterMessage = CounterMessage;
            
            await _context.SaveChangesAsync();

            // Notify Provider
            var provider = await _context.ServiceProviders.Include(p => p.User).FirstOrDefaultAsync(p => p.Id == bid.ServiceProviderId);
            if (provider != null)
            {
                var notif = new Notification
                {
                    UserId = provider.UserId,
                    Title = "Counter Offer Received",
                    Message = $"Customer {user.FullName} has proposed a counter-offer of Rs. {CounterPrice} for your bid on '{bid.ServiceRequest.Title}'",
                    Type = NotificationType.System,
                    CreatedAt = DateTime.UtcNow,
                    RelatedEntityId = bid.ServiceRequestId,
                    RelatedEntityType = "ServiceRequest"
                };
                _context.Notifications.Add(notif);
                await _context.SaveChangesAsync();
            }

            TempData["SuccessMessage"] = "Your counter-offer has been sent to the provider!";
            return RedirectToAction(nameof(Details), new { id = bid.ServiceRequestId });
        }
    }
}
