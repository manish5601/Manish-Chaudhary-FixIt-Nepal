using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FixItNepal.Models
{
    public enum VerificationStatus
    {
        Pending,
        Approved,
        Rejected,
        Suspended
    }

    public enum ServiceCategory
    {
        Plumbing,
        Electrical,
        Carpentry,
        Painting,
        Cleaning,
        ACRepair,
        Other
    }

    public class ServiceProvider
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; } = null!;

        [Required]
        public ServiceCategory PrimaryService { get; set; }

        public string? Skills { get; set; } // Comma-separated additional skills

        [Required]
        public int ExperienceYears { get; set; }

        public string? ServiceAreas { get; set; } // Comma-separated locations

        // Pricing handled by Admin via ServiceItem

        public VerificationStatus Status { get; set; } = VerificationStatus.Pending;

        public string? RejectionReason { get; set; }

        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
        public DateTime? VerifiedAt { get; set; }

        public string? VerifiedBy { get; set; } // Admin UserId

        // Availability (JSON or separate table in v2)
        public string? AvailabilityJson { get; set; }

        // Rating
        public decimal AverageRating { get; set; } = 0;
        public int TotalReviews { get; set; } = 0;

        // Navigation
        public ICollection<ProviderDocument> Documents { get; set; } = new List<ProviderDocument>();
        // public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    }
}
