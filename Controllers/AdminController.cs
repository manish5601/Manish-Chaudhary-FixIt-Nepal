using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FixItNepal.Models;
using FixItNepal.Data;
using FixItNepal.ViewModels;
using FixItNepal.Services;
using Microsoft.AspNetCore.Identity;

namespace FixItNepal.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;

        public AdminController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IEmailService emailService)
        {
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
        }

        public async Task<IActionResult> Dashboard()
        {
            var totalUsers = await _context.Users.CountAsync();
            var activeProviders = await _context.ServiceProviders.CountAsync(p => p.Status == VerificationStatus.Approved);
            var pendingProviders = await _context.ServiceProviders.CountAsync(p => p.Status == VerificationStatus.Pending);
            
            // Booking Stats
            var today = DateTime.UtcNow.Date;
            var bookingsToday = await _context.Bookings.CountAsync(b => b.CreatedAt >= today);
            var totalBookings = await _context.Bookings.CountAsync();
            var completedBookings = await _context.Bookings.CountAsync(b => b.Status == BookingStatus.Completed);
            var pendingBookings = await _context.Bookings.CountAsync(b => b.Status == BookingStatus.Pending);
            var cancelledBookings = await _context.Bookings.CountAsync(b => b.Status == BookingStatus.Cancelled);

            // Revenue (Only from completed bookings)
            var totalRevenue = await _context.Bookings
                .Where(b => b.Status == BookingStatus.Completed)
                .SumAsync(b => b.TotalPrice);

            // Category Insights
            var topCategories = await _context.Bookings
                .Include(b => b.ServiceItem).ThenInclude(si => si.ServiceCategory)
                .Where(b => b.Status == BookingStatus.Completed)
                .GroupBy(b => b.ServiceItem.ServiceCategory.Name)
                .Select(g => new CategoryInsight
                {
                    CategoryName = g.Key,
                    BookingCount = g.Count(),
                    Revenue = g.Sum(b => b.TotalPrice)
                })
                .OrderByDescending(c => c.BookingCount)
                .Take(5)
                .ToListAsync();

            // Provider Insights
            var topProviders = await _context.ServiceProviders
                .Include(p => p.User)
                .Where(p => p.Status == VerificationStatus.Approved && p.TotalReviews > 0)
                .OrderByDescending(p => p.AverageRating)
                .ThenByDescending(p => p.TotalReviews)
                .Take(5)
                .Select(p => new ProviderInsight
                {
                    ProviderName = p.User.FullName,
                    AverageRating = p.AverageRating,
                    TotalReviews = p.TotalReviews,
                    CompletedBookings = _context.Bookings.Count(b => b.ServiceProviderId == p.Id && b.Status == BookingStatus.Completed)
                })
                .ToListAsync();

            // Fetch recent activities
            var recentProvidersData = await _context.ServiceProviders
                .Include(p => p.User)
                .OrderByDescending(p => p.RegisteredAt)
                .Take(3)
                .ToListAsync();

            var recentBookingsData = await _context.Bookings
                .Include(b => b.Customer).ThenInclude(c => c.User)
                .OrderByDescending(b => b.CreatedAt)
                .Take(5)
                .ToListAsync();

            var recentActivities = new List<DashboardActivity>();

            recentActivities.AddRange(recentProvidersData.Select(p => new DashboardActivity
            {
                UserName = p.User.FullName,
                Action = "Joined as Provider",
                TimeAgo = GetTimeAgo(p.RegisteredAt),
                StatusText = p.Status.ToString(),
                StatusColor = p.Status == VerificationStatus.Pending ? "bg-warning-subtle text-warning" : 
                              p.Status == VerificationStatus.Approved ? "bg-success-subtle text-success" : "bg-danger-subtle text-danger"
            }));

            recentActivities.AddRange(recentBookingsData.Select(b => new DashboardActivity
            {
                UserName = b.Customer.User.FullName,
                Action = $"Booked Service #{b.Id}",
                TimeAgo = GetTimeAgo(b.CreatedAt),
                StatusText = b.Status.ToString(),
                StatusColor = b.Status == BookingStatus.Completed ? "bg-success-subtle text-success" :
                              b.Status == BookingStatus.Cancelled ? "bg-danger-subtle text-danger" :
                              b.Status == BookingStatus.Pending ? "bg-warning-subtle text-warning" : "bg-info-subtle text-info"
            }));

            var model = new AdminDashboardViewModel 
            {
                TotalUsers = totalUsers,
                ActiveProviders = activeProviders,
                PendingProviders = pendingProviders,
                BookingsToday = bookingsToday,
                TotalBookings = totalBookings,
                CompletedBookings = completedBookings,
                PendingBookings = pendingBookings,
                CancelledBookings = cancelledBookings,
                TotalRevenue = totalRevenue,
                TopCategories = topCategories,
                TopProviders = topProviders,
                RecentActivities = recentActivities.OrderByDescending(a => a.TimeAgo).Take(10).ToList() // Note: TimeAgo sorting is strings, might want to refine Activity model but OK for now as we just merged lists.
            };

            return View(model);
        }

        private static string GetTimeAgo(DateTime dateTime)
        {
            var span = DateTime.UtcNow - dateTime;
            if (span.Days > 30) return "Months ago";
            if (span.Days > 0) return $"{span.Days} days ago";
            if (span.Hours > 0) return $"{span.Hours} hours ago";
            if (span.Minutes > 0) return $"{span.Minutes} mins ago";
            return "Just now";
        }

        // GET: /Admin/VerifyProviders
        public async Task<IActionResult> VerifyProviders()
        {
            var pendingProviders = await _context.ServiceProviders
                .Include(p => p.User)
                .Include(p => p.ServiceCategory)
                .Include(p => p.Documents)
                .Where(p => p.Status == VerificationStatus.Pending)
                .ToListAsync();

            return View(pendingProviders);
        }

        // POST: /Admin/ApproveProvider/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveProvider(int id)
        {
            var provider = await _context.ServiceProviders.FindAsync(id);
            if (provider == null) return NotFound();

            provider.Status = VerificationStatus.Approved;
            provider.VerifiedAt = DateTime.UtcNow;
            provider.VerifiedBy = User.Identity?.Name;

            _context.Update(provider);
            await _context.SaveChangesAsync();

            // Send Email to Provider
            var providerUser = await _userManager.FindByIdAsync(provider.UserId);
            if (providerUser != null)
            {
                var subject = "Account Approved - FixIt Nepal";
                var body = $@"
                    <h2>Account Approved</h2>
                    <p>Hello {providerUser.FullName},</p>
                    <p>Congratulations! Your service provider account has been <strong>Approved</strong>.</p>
                    <p>You can now log in and start accepting booking requests.</p>
                    <p>Welcome to the FixIt Nepal team!</p>
                ";
                await _emailService.SendEmailAsync(providerUser.Email, subject, body);
            }

            return RedirectToAction(nameof(VerifyProviders));
        }

        // POST: /Admin/RejectProvider/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectProvider(int id, string rejectionReason)
        {
            var provider = await _context.ServiceProviders.FindAsync(id);
            if (provider == null) return NotFound();

            provider.Status = VerificationStatus.Rejected;
            provider.RejectionReason = rejectionReason;

            _context.Update(provider);
            await _context.SaveChangesAsync();

            // Send Email to Provider
            var providerUser = await _userManager.FindByIdAsync(provider.UserId);
            if (providerUser != null)
            {
                var subject = "Account Verification Update - FixIt Nepal";
                var body = $@"
                    <h2>Account Verification Update</h2>
                    <p>Hello {providerUser.FullName},</p>
                    <p>We have reviewed your application and unfortunately, it has been <strong>Rejected</strong> at this time.</p>
                    <p><strong>Reason:</strong> {rejectionReason}</p>
                    <p>Please address the reason above and update your profile or documents for re-verification.</p>
                ";
                await _emailService.SendEmailAsync(providerUser.Email, subject, body);
            }

            return RedirectToAction(nameof(VerifyProviders));
        }

        // GET: /Admin/UserManagement
        public async Task<IActionResult> UserManagement(string role, string search)
        {
            var query = _context.Users.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(u => u.Email.Contains(search) || u.FullName.Contains(search));
            }

            var users = await query.ToListAsync();
            var userViewModels = new List<UserManagementViewModel>();

            foreach (var user in users)
            {
                var roles = await _context.UserRoles
                    .Where(ur => ur.UserId == user.Id)
                    .Join(_context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name)
                    .ToListAsync();
                
                // Exclude current admin from list to prevent self-lockout
                if (user.UserName == User.Identity.Name) continue;

                if (!string.IsNullOrEmpty(role) && !roles.Contains(role)) continue;
                
                string providerStatus = null;
                if (roles.Contains("ServiceProvider"))
                {
                    var provider = await _context.ServiceProviders.FirstOrDefaultAsync(p => p.UserId == user.Id);
                    providerStatus = provider?.Status.ToString();
                }

                userViewModels.Add(new UserManagementViewModel
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    Role = string.Join(", ", roles),
                    IsActive = user.IsActive,
                    ProfilePicture = user.ProfilePicture,
                    ProviderStatus = providerStatus
                });
            }

            ViewBag.CurrentRole = role;
            ViewBag.CurrentSearch = search;
            return View(userViewModels);
        }

        // POST: /Admin/ToggleUserStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleUserStatus(string id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            // Prevent Action on Super Admin
            if (user.Email == "admin@fixitnepal.com") 
            {
                 // TempData logic can be handled in View or simplified
                 return RedirectToAction(nameof(UserManagement));
            }

            user.IsActive = !user.IsActive;
            _context.Update(user);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(UserManagement));
        }

        // POST: /Admin/DeleteUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            if (user.Email == "admin@fixitnepal.com") return RedirectToAction(nameof(UserManagement));

            // Remove relevant related entities manually if needed
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == id);
            if (customer != null) _context.Customers.Remove(customer);

            var provider = await _context.ServiceProviders.FirstOrDefaultAsync(p => p.UserId == id);
            if (provider != null) _context.ServiceProviders.Remove(provider);

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(UserManagement));
        }

        // --- Moderation & Disputes ---

        // GET: /Admin/Moderation
        public async Task<IActionResult> Moderation()
        {
            var flaggedReviews = await _context.Reviews
                .Include(r => r.Customer).ThenInclude(c => c.User)
                .Include(r => r.ServiceProvider).ThenInclude(p => p.User)
                .Include(r => r.Booking)
                .Where(r => r.IsFlagged)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return View(flaggedReviews);
        }

        // POST: /Admin/DismissFlag/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DismissFlag(int id)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review == null) return NotFound();

            review.IsFlagged = false;
            review.AdminNote = "Flag dismissed by admin.";
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Moderation));
        }

        // POST: /Admin/DeleteReview/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteReview(int id)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review == null) return NotFound();

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

            return RedirectToAction(nameof(Moderation));
        }

        // GET: /Admin/Disputes
        public async Task<IActionResult> Disputes()
        {
            var disputes = await _context.Disputes
                .Include(d => d.Booking)
                    .ThenInclude(b => b.Customer).ThenInclude(c => c.User)
                .Include(d => d.Booking)
                    .ThenInclude(b => b.ServiceProvider).ThenInclude(p => p.User)
                .Include(d => d.Booking)
                    .ThenInclude(b => b.ServiceItem)
                .Include(d => d.RaisedBy)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();

            return View(disputes);
        }

        // POST: /Admin/ResolveDispute
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResolveDispute(int id, DisputeStatus status, string resolution)
        {
            var dispute = await _context.Disputes
                .Include(d => d.Booking)
                .Include(d => d.RaisedBy)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (dispute == null) return NotFound();

            dispute.Status = status;
            dispute.Resolution = resolution;
            dispute.ResolvedAt = DateTime.UtcNow;
            dispute.ResolvedBy = _userManager.GetUserId(User);

            // Notify party who raised the dispute
            _context.Notifications.Add(new Notification
            {
                UserId = dispute.RaisedById,
                Title = "Dispute Resolved",
                Message = $"Your dispute for Booking #{dispute.BookingId} has been {status}. Resolution: {resolution}",
                RelatedEntityId = dispute.Id,
                RelatedEntityType = "Dispute"
            });

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Disputes));
        }

        // GET: /Admin/Bookings
        public async Task<IActionResult> Bookings(BookingStatus? status, string search)
        {
            var query = _context.Bookings
                .Include(b => b.Customer).ThenInclude(c => c.User)
                .Include(b => b.ServiceProvider).ThenInclude(p => p.User)
                .Include(b => b.ServiceItem)
                .AsQueryable();

            if (status.HasValue)
            {
                query = query.Where(b => b.Status == status.Value);
            }

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(b => b.Customer.User.FullName.Contains(search) || 
                                        b.ServiceProvider.User.FullName.Contains(search) ||
                                        b.Id.ToString() == search);
            }

            var bookings = await query.OrderByDescending(b => b.CreatedAt).ToListAsync();
            ViewBag.CurrentStatus = status;
            ViewBag.CurrentSearch = search;
            return View(bookings);
        }

        // GET: /Admin/ViewCustomerProfile/id
        public async Task<IActionResult> ViewCustomerProfile(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == id);
            if (customer == null) return NotFound("Customer record not found.");

            var model = new CustomerProfileViewModel
            {
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Address = user.Address,
                PreferredLocation = customer.PreferredLocation,
                ProfilePicture = user.ProfilePicture
            };

            ViewBag.AdminView = true;
            return View("~/Views/Customer/Profile.cshtml", model);
        }

        // GET: /Admin/ViewProviderProfile/id
        public async Task<IActionResult> ViewProviderProfile(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var provider = await _context.ServiceProviders
                .Include(p => p.Documents)
                .Include(p => p.ServiceCategory)
                .FirstOrDefaultAsync(p => p.UserId == id);

            if (provider == null) return NotFound("Provider record not found.");

            var model = new ProviderProfileViewModel
            {
                FullName = user.FullName,
                PhoneNumber = user.PhoneNumber,
                Address = user.Address,
                Email = user.Email,
                ProfilePicture = user.ProfilePicture,
                ServiceCategoryId = provider.ServiceCategoryId,
                ServiceCategoryName = provider.ServiceCategory?.Name,
                ExperienceYears = provider.ExperienceYears,
                ServiceAreas = provider.ServiceAreas,
                Skills = provider.Skills,
                Status = provider.Status,
                Latitude = provider.Latitude,
                Longitude = provider.Longitude,
                Documents = provider.Documents.ToList()
            };

            ViewBag.AdminView = true;
            return View("~/Views/ServiceProvider/Profile.cshtml", model);
        }
    }
}
