using FixItNepal.Data;
using FixItNepal.Models;
using FixItNepal.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FixItNepal.Controllers
{
    [Authorize(Roles = "ServiceProvider")]
    public class ServiceProviderController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public ServiceProviderController(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public async Task<IActionResult> Dashboard()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");
            
            var provider = await _context.ServiceProviders
                .Include(p => p.Documents)
                .FirstOrDefaultAsync(p => p.UserId == user.Id);

            ViewBag.Status = provider?.Status ?? VerificationStatus.Pending;
            
            return View();
        }

        // GET: ServiceProvider/Profile
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var provider = await _context.ServiceProviders
                .Include(p => p.Documents)
                .FirstOrDefaultAsync(p => p.UserId == user.Id);

            if (provider == null) return NotFound("Provider record not found");

            var model = new ProviderProfileViewModel
            {
                FullName = user.FullName,
                PhoneNumber = user.PhoneNumber,
                Address = user.Address,
                Email = user.Email,
                ProfilePicture = user.ProfilePicture,
                PrimaryService = provider.PrimaryService,
                ExperienceYears = provider.ExperienceYears,
                ServiceAreas = provider.ServiceAreas,
                Skills = provider.Skills,
                Status = provider.Status,
                Documents = provider.Documents.ToList()
            };

            return View(model);
        }

        // POST: ServiceProvider/Profile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(ProviderProfileViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var provider = await _context.ServiceProviders.FirstOrDefaultAsync(p => p.UserId == user.Id);
            if (provider == null) return NotFound();

            if (ModelState.IsValid)
            {
                // Update User Info
                user.FullName = model.FullName;
                user.PhoneNumber = model.PhoneNumber;
                user.Address = model.Address;
                await _userManager.UpdateAsync(user);

                // Update Provider Info
                 // Note: PrimaryService usually shouldn't be changed easily as documents are tied to it, but keeping flexible for now or lock it.
                provider.ServiceAreas = model.ServiceAreas;
                provider.Skills = model.Skills;
                provider.ExperienceYears = model.ExperienceYears;
                
                _context.Update(provider);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Profile updated successfully!";
                return RedirectToAction(nameof(Profile));
            }
            return View(model);
        }
    }
}
