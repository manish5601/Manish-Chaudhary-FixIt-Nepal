using FixItNepal.Models;
using FixItNepal.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.IO;
using FixItNepal.Services;
using ServiceProviderModel = FixItNepal.Models.ServiceProvider;

namespace FixItNepal.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly Data.ApplicationDbContext _context;
        private readonly IEmailService _emailService;

        public AccountController(UserManager<ApplicationUser> userManager,
                                 SignInManager<ApplicationUser> signInManager,
                                 Data.ApplicationDbContext context,
                                 IEmailService emailService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _emailService = emailService;
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
            var model = new ProviderRegistrationViewModel
            {
                Categories = _context.ServiceCategories
                    .Where(c => c.IsActive)
                    .Select(c => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                    {
                        Value = c.Id.ToString(),
                        Text = c.Name
                    }).ToList()
            };
            return View(model);
        }
        
        // POST: /Account/RegisterProvider
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterProvider(ProviderRegistrationViewModel model)
        {
            if (!ModelState.IsValid) 
            {
                model.Categories = _context.ServiceCategories
                    .Where(c => c.IsActive)
                    .Select(c => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                    {
                        Value = c.Id.ToString(),
                        Text = c.Name
                    }).ToList();
                return View(model);
            }

            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("", "User already exists.");
                model.Categories = _context.ServiceCategories
                    .Where(c => c.IsActive)
                    .Select(c => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                    {
                        Value = c.Id.ToString(),
                        Text = c.Name
                    }).ToList();
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
                    ServiceCategoryId = model.ServiceCategoryId,
                    ExperienceYears = model.ExperienceYears,
                    ServiceAreas = model.ServiceAreas,
                    Skills = model.Skills,
                    // PricingType = "Hourly", Removed
                    Status = VerificationStatus.Pending,
                    RegisteredAt = DateTime.UtcNow,
                    Latitude = model.Latitude,
                    Longitude = model.Longitude
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

            model.Categories = _context.ServiceCategories
                    .Where(c => c.IsActive)
                    .Select(c => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                    {
                        Value = c.Id.ToString(),
                        Text = c.Name
                    }).ToList();

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

        // GET: /Account/ForgotPassword
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // POST: /Account/ForgotPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                // Don't reveal that the user does not exist
                return RedirectToAction("ForgotPasswordConfirmation");
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var callbackUrl = Url.Action("ResetPassword", "Account", new { userId = user.Id, token = token }, protocol: Request.Scheme);

            var subject = "Reset Password - FixIt Nepal";
            var body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #eee; border-radius: 10px;'>
                    <h2 style='color: #fd7e14;'>Reset Your Password</h2>
                    <p>Hello {user.FullName},</p>
                    <p>We received a request to reset the password for your FixIt Nepal account. Click the button below to set a new password:</p>
                    <div style='text-align: center; margin: 30px 0;'>
                        <a href='{callbackUrl}' style='background-color: #fd7e14; color: white; padding: 12px 25px; text-decoration: none; border-radius: 5px; font-weight: bold;'>Reset Password</a>
                    </div>
                    <p>If you did not request a password reset, please ignore this email.</p>
                    <p style='color: #888; font-size: 12px;'>This link will expire in 24 hours.</p>
                </div>";

            await _emailService.SendEmailAsync(model.Email, subject, body);

            return RedirectToAction("ForgotPasswordConfirmation");
        }

        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        // GET: /Account/ResetPassword
        public IActionResult ResetPassword(string? token = null, string? userId = null)
        {
            if (token == null || userId == null) return BadRequest("A token and user ID must be supplied for password reset.");
            
            var model = new ResetPasswordViewModel { Token = token, UserId = userId };
            return View(model);
        }

        // POST: /Account/ResetPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
            {
                // Don't reveal that the user does not exist
                return RedirectToAction("ResetPasswordConfirmation");
            }

            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);
            if (result.Succeeded)
            {
                return RedirectToAction("ResetPasswordConfirmation");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }
            return View(model);
        }

        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }
    }
}
