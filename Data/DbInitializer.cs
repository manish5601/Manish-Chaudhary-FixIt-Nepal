using FixItNepal.Models;
using Microsoft.AspNetCore.Identity;

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
        }
    }
}
