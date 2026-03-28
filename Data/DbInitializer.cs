using FixItNepal.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FixItNepal.Data
{
    public static class DbInitializer
    {
        public static async Task SeedRolesAndAdmin(IServiceProvider services)
        {
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

            string[] roles = { "Admin", "Customer", "ServiceProvider" };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }

            // Create default admin
            var adminEmail = "admin@fixitnepal.com";
            if (await userManager.FindByEmailAsync(adminEmail) == null)
            {
                var admin = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "System Administrator",
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(admin, "Admin@123");
                if (result.Succeeded)
                    await userManager.AddToRoleAsync(admin, "Admin");
            }

            // Seed Service Categories
            var context = services.GetRequiredService<ApplicationDbContext>();
             if (!context.ServiceCategories.Any())
            {
                var categories = new List<ServiceCategory>
                {
                    new ServiceCategory { Name = "Plumbing", Description = "Pipe repairs and installation", IconPath = "bi-droplet", IsActive = true },
                    new ServiceCategory { Name = "Electrical", Description = "Wiring and appliance repair", IconPath = "bi-lightning", IsActive = true },
                    new ServiceCategory { Name = "Cleaning", Description = "Home and office cleaning", IconPath = "bi-stars", IsActive = true },
                    new ServiceCategory { Name = "Painting", Description = "Wall painting and decoration", IconPath = "bi-paint-bucket", IsActive = true },
                    new ServiceCategory { Name = "Carpentry", Description = "Furniture repair and assembly", IconPath = "bi-hammer", IsActive = true }
                };

                context.ServiceCategories.AddRange(categories);
                await context.SaveChangesAsync();
            }

            // Seed Service Items
            if (!context.ServiceItems.Any())
            {
                var plumbing = context.ServiceCategories.FirstOrDefault(c => c.Name == "Plumbing");
                var electrical = context.ServiceCategories.FirstOrDefault(c => c.Name == "Electrical");

                if (plumbing != null && electrical != null)
                {
                    var serviceItems = new List<ServiceItem>
                    {
                        new ServiceItem { Name = "Pipe Leak Repair", Description = "Fixing leaking pipes", BasePrice = 500, ServiceCategoryId = plumbing.Id, IsActive = true },
                        new ServiceItem { Name = "Tap Installation", Description = "Install new water tap", BasePrice = 300, ServiceCategoryId = plumbing.Id, IsActive = true },
                        new ServiceItem { Name = "Switch Replacement", Description = "Replace damaged switch", BasePrice = 200, ServiceCategoryId = electrical.Id, IsActive = true },
                        new ServiceItem { Name = "Fan Installation", Description = "Ceiling fan installation", BasePrice = 400, ServiceCategoryId = electrical.Id, IsActive = true }
                    };
                    
                    context.ServiceItems.AddRange(serviceItems);
                    await context.SaveChangesAsync();
                }
            }

            // Seed Kathmandu Provider
            if (await userManager.FindByEmailAsync("testprovider@fixitnepal.com") == null)
            {
                var providerUser = new ApplicationUser
                {
                    UserName = "testprovider@fixitnepal.com",
                    Email = "testprovider@fixitnepal.com",
                    FullName = "Kathmandu Plumbing Expert",
                    PhoneNumber = "9800000000",
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(providerUser, "Provider@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(providerUser, "ServiceProvider");
                    var plumbing = context.ServiceCategories.FirstOrDefault(c => c.Name == "Plumbing");
                    context.ServiceProviders.Add(new FixItNepal.Models.ServiceProvider
                    {
                        UserId = providerUser.Id,
                        ServiceCategoryId = plumbing?.Id ?? 0,
                        ExperienceYears = 5,
                        Status = VerificationStatus.Approved,
                        Address = "Kathmandu, Nepal",
                        Latitude = 27.7172,
                        Longitude = 85.3240,
                        AverageRating = 4.5m,
                        TotalReviews = 1
                    });
                    await context.SaveChangesAsync();
                }
            }

            // Seed Inaruwa Provider
            if (await userManager.FindByEmailAsync("inaruwa@fixitnepal.com") == null)
            {
                var inaruwaUser = new ApplicationUser
                {
                    UserName = "inaruwa@fixitnepal.com",
                    Email = "inaruwa@fixitnepal.com",
                    FullName = "Inaruwa Repairs (Test)",
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(inaruwaUser, "Admin123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(inaruwaUser, "ServiceProvider");
                    var cleaning = context.ServiceCategories.FirstOrDefault(c => c.Name == "Cleaning");
                    context.ServiceProviders.Add(new FixItNepal.Models.ServiceProvider
                    {
                        UserId = inaruwaUser.Id,
                        ServiceCategoryId = cleaning?.Id ?? 0,
                        ExperienceYears = 3,
                        Status = VerificationStatus.Approved,
                        Address = "Inaruwa, Nepal",
                        Latitude = 26.6,
                        Longitude = 87.15,
                        AverageRating = 4.8m,
                        TotalReviews = 5
                    });
                    await context.SaveChangesAsync();
                }
            }

            // Automatically Approve 'Mahan' if they exist so they show on map
            var mahan = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(
                context.ServiceProviders.Include(p => p.User), 
                p => p.User.FullName.Contains("mahan") || p.User.UserName.Contains("mahan"));

            if (mahan != null && mahan.Status != VerificationStatus.Approved)
            {
                mahan.Status = VerificationStatus.Approved;
                // Ensure they have coords even if registration failed to capture them
                if (!mahan.Latitude.HasValue) mahan.Latitude = 26.5958;
                if (!mahan.Longitude.HasValue) mahan.Longitude = 87.1467;
                await context.SaveChangesAsync();
            }
        }
    }
}
