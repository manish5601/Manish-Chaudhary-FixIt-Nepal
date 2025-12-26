using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FixItNepal.Models;
using FixItNepal.Data;

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

        public IActionResult Dashboard()
        {
            return View();
        }

        // GET: /Admin/VerifyProviders
        public async Task<IActionResult> VerifyProviders()
        {
            var pendingProviders = await _context.ServiceProviders
                .Include(p => p.User)
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
    }
}
