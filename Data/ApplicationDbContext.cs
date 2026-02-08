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
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<ProviderAvailability> ProviderAvailabilities { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            
            builder.Entity<Customer>().ToTable("Customers");
            builder.Entity<FixItNepal.Models.ServiceProvider>().ToTable("ServiceProviders");
            builder.Entity<ProviderDocument>().ToTable("ProviderDocuments");
            builder.Entity<ServiceItem>().ToTable("ServiceItems");
            builder.Entity<ServiceCategory>().ToTable("ServiceCategories");
            builder.Entity<Booking>().ToTable("Bookings");
            builder.Entity<Notification>().ToTable("Notifications");
            builder.Entity<ProviderAvailability>().ToTable("ProviderAvailabilities");

            // Configure Booking Relationships (Prevent Cascade Cycles)
            builder.Entity<Booking>()
                .HasOne(b => b.Customer)
                .WithMany()
                .HasForeignKey(b => b.CustomerId)
                .OnDelete(DeleteBehavior.Restrict); // Do not delete booking if customer is deleted (preserve history)

            builder.Entity<Booking>()
                .HasOne(b => b.ServiceProvider)
                .WithMany()
                .HasForeignKey(b => b.ServiceProviderId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Booking>()
                .HasOne(b => b.ServiceItem)
                .WithMany()
                .HasForeignKey(b => b.ServiceItemId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Decimal Precision
            builder.Entity<ServiceItem>()
                .Property(s => s.BasePrice)
                .HasColumnType("decimal(18,2)");

            builder.Entity<Booking>()
                .Property(b => b.TotalPrice)
                .HasColumnType("decimal(18,2)");

            builder.Entity<FixItNepal.Models.ServiceProvider>()
                .Property(p => p.AverageRating)
                .HasColumnType("decimal(3,2)"); // 0.00 to 5.00
        }
    }
}
