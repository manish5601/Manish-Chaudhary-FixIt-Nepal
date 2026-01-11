using FixItNepal.Models;
using FixItNepal.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using ServiceProviderModel = FixItNepal.Models.ServiceProvider;

namespace FixItNepal.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly Data.ApplicationDbContext _context;

        public AccountController(UserManager<ApplicationUser> userManager,
                                 SignInManager<ApplicationUser> signInManager,
                                 Data.ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }

        // GET: /Account/Register
        // GET: /Account/Register
        public IActionResult Register()
        {
            // Show Role Selection Page
            return View("RoleSelection");
        }

        // GET: /Account/RegisterCustomer
        [HttpGet]
        public IActionResult RegisterCustomer()
        {
            return View();
        }

        // POST: /Account/RegisterCustomer
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterCustomer(CustomerRegistrationViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("", "User already exists.");
                return View(model);
            }

            // Profile Picture Logic
            string profilePicPath = null;
            if (model.ProfileImage != null && model.ProfileImage.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/profiles");
                Directory.CreateDirectory(uploadsFolder);
                var uniqueFileName = Guid.NewGuid().ToString() + "_" + model.ProfileImage.FileName;
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await model.ProfileImage.CopyToAsync(fileStream);
                }
                profilePicPath = uniqueFileName;
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                Address = model.Address,
                PhoneNumber = model.PhoneNumber,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                ProfilePicture = profilePicPath
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "Customer");

                // Create Customer Record
                var customer = new Customer
                {
                    UserId = user.Id,
                    PreferredLocation = model.PreferredLocation,
                    RegisteredAt = DateTime.UtcNow
                };

                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();

                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToAction("Dashboard", "Customer");
            }
            
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return View(model);
        }

        // GET: /Account/RegisterProvider
        [HttpGet]
        public IActionResult RegisterProvider()
        {
            return View();
        }
        
        // POST: /Account/RegisterProvider
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterProvider(ProviderRegistrationViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("", "User already exists.");
                return View(model);
            }

            // Profile Picture Logic
            string profilePicPath = null;
            if (model.ProfileImage != null && model.ProfileImage.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/profiles");
                Directory.CreateDirectory(uploadsFolder);
                var uniqueFileName = Guid.NewGuid().ToString() + "_" + model.ProfileImage.FileName;
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await model.ProfileImage.CopyToAsync(fileStream);
                }
                profilePicPath = uniqueFileName;
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                Address = model.Address,
                PhoneNumber = model.PhoneNumber,
                IsActive = true, // User is active, but Provider status is pending
                CreatedAt = DateTime.UtcNow,
                ProfilePicture = profilePicPath
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "ServiceProvider");
                
                // File Upload Logic
                string documentPath = "";
                if (model.IdentificationDocument != null && model.IdentificationDocument.Length > 0)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/documents");
                    Directory.CreateDirectory(uploadsFolder);
                    var uniqueFileName = Guid.NewGuid().ToString() + "_" + model.IdentificationDocument.FileName;
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.IdentificationDocument.CopyToAsync(fileStream);
                    }
                    documentPath = uniqueFileName; // Store relative path or filename
                }

                // Create ServiceProvider and Document entities
                var serviceProvider = new ServiceProviderModel
                {
                    UserId = user.Id,
                    PrimaryService = model.PrimaryService,
                    ExperienceYears = model.ExperienceYears,
                    ServiceAreas = model.ServiceAreas,
                    Skills = model.Skills,
                    // PricingType = "Hourly", Removed
                    Status = VerificationStatus.Pending,
                    RegisteredAt = DateTime.UtcNow
                };

                _context.ServiceProviders.Add(serviceProvider);
                await _context.SaveChangesAsync();

                if (!string.IsNullOrEmpty(documentPath))
                {
                    var document = new ProviderDocument
                    {
                        ServiceProviderId = serviceProvider.Id,
                        Type = DocumentType.CitizenshipCard, // Default for now
                        FilePath = documentPath,
                        UploadedAt = DateTime.UtcNow,
                        IsVerified = false
                    };
                    _context.ProviderDocuments.Add(document);
                    await _context.SaveChangesAsync();
                }

                await _signInManager.SignInAsync(user, isPersistent: false);
                // Redirect to a specific "Pending Verification" page or Dashboard
                return RedirectToAction("Dashboard", "ServiceProvider");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }
            return View(model);
        }

        public IActionResult Login()
        {
            return View();
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid) return View(model);

            var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user != null)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    if (roles.Contains("Admin")) return RedirectToAction("Dashboard", "Admin");
                    if (roles.Contains("Customer")) return RedirectToAction("Dashboard", "Customer");
                    if (roles.Contains("ServiceProvider")) 
                    {
                        // Check Verification Status here later
                        return RedirectToAction("Dashboard", "ServiceProvider");
                    }
                }
                
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }
    }
}
