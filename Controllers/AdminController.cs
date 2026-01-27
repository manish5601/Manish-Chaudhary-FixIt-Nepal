using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FixItNepal.Models;
using FixItNepal.Data;
using FixItNepal.ViewModels;

namespace FixItNepal.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Dashboard()
        {
            var totalUsers = await _context.Users.CountAsync();
            var activeProviders = await _context.ServiceProviders.CountAsync(p => p.Status == VerificationStatus.Approved);
            var pendingProviders = await _context.ServiceProviders.CountAsync(p => p.Status == VerificationStatus.Pending);
            var bookingsToday = 0; // Placeholder

            // Fetch recent activities (Fetch entities first to avoid EF translation issues with GetTimeAgo/ToString)
            var recentProvidersData = await _context.ServiceProviders
                .Include(p => p.User)
                .OrderByDescending(p => p.RegisteredAt)
                .Take(5)
                .ToListAsync();

            var recentCustomersData = await _context.Customers
                .Include(c => c.User)
                .OrderByDescending(c => c.RegisteredAt)
                .Take(5)
                .ToListAsync();

            var recentProviders = recentProvidersData.Select(p => new DashboardActivity
                {
                    UserName = p.User.FullName,
                    Action = "Registered as Provider",
                    TimeAgo = GetTimeAgo(p.RegisteredAt),
                    StatusText = p.Status.ToString(),
                    StatusColor = p.Status == VerificationStatus.Pending ? "bg-warning-subtle text-warning" : 
                                  p.Status == VerificationStatus.Approved ? "bg-success-subtle text-success" : "bg-danger-subtle text-danger"
                });

            var recentCustomers = recentCustomersData.Select(c => new DashboardActivity
                {
                    UserName = c.User.FullName,
                    Action = "Joined as Customer",
                    TimeAgo = GetTimeAgo(c.RegisteredAt),
                    StatusText = "Active",
                    StatusColor = "bg-primary-subtle text-primary"
                });

            // Merge and sort in memory
            var activities = recentProviders.Concat(recentCustomers)
                .ToList(); // Determine sort order if needed, or just mix
            
            var model = new AdminDashboardViewModel // Removed ViewModels. prefix
            {
                TotalUsers = totalUsers,
                ActiveProviders = activeProviders,
                PendingProviders = pendingProviders,
                BookingsToday = bookingsToday,
                RecentActivities = activities.Take(5).ToList()
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

                userViewModels.Add(new UserManagementViewModel
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    Role = string.Join(", ", roles),
                    IsActive = user.IsActive,
                    ProfilePicture = user.ProfilePicture
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
    }
}
