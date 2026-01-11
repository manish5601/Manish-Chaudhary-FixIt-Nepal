using FixItNepal.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FixItNepal.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Customer> Customers { get; set; }
        public DbSet<FixItNepal.Models.ServiceProvider> ServiceProviders { get; set; }
        public DbSet<ProviderDocument> ProviderDocuments { get; set; }
        public DbSet<ServiceItem> ServiceItems { get; set; }
    }
}
