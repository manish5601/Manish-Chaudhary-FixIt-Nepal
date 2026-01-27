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
        public DbSet<ServiceCategory> ServiceCategories { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            
            builder.Entity<Customer>().ToTable("Customers");
            builder.Entity<FixItNepal.Models.ServiceProvider>().ToTable("ServiceProviders");
            builder.Entity<ProviderDocument>().ToTable("ProviderDocuments");
            builder.Entity<ServiceItem>().ToTable("ServiceItems");
            builder.Entity<ServiceCategory>().ToTable("ServiceCategories");
        }
    }
}
