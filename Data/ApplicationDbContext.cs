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
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Dispute> Disputes { get; set; }

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
            builder.Entity<Review>().ToTable("Reviews");
            builder.Entity<Dispute>().ToTable("Disputes");

            // Configure Booking Relationships (Prevent Cascade Cycles)
            builder.Entity<Booking>()
                .HasOne(b => b.Customer)
                .WithMany()
                .HasForeignKey(b => b.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

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

            // Review Relationships (1-to-1 with Booking)
            builder.Entity<Booking>()
                .HasOne(b => b.Review)
                .WithOne(r => r.Booking)
                .HasForeignKey<Review>(r => r.BookingId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Review>()
                .HasOne(r => r.Customer)
                .WithMany()
                .HasForeignKey(r => r.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Review>()
                .HasOne(r => r.ServiceProvider)
                .WithMany()
                .HasForeignKey(r => r.ServiceProviderId)
                .OnDelete(DeleteBehavior.Restrict);

            // Dispute Relationships
            builder.Entity<Dispute>()
                .HasOne(d => d.Booking)
                .WithMany()
                .HasForeignKey(d => d.BookingId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Dispute>()
                .HasOne(d => d.RaisedBy)
                .WithMany()
                .HasForeignKey(d => d.RaisedById)
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
