using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FixItNepal.Models
{
    public enum DisputeStatus
    {
        Open,
        UnderReview,
        Resolved,
        Rejected
    }

    public class Dispute
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int BookingId { get; set; }

        [ForeignKey("BookingId")]
        public Booking Booking { get; set; } = null!;

        [Required]
        public string RaisedById { get; set; } = string.Empty;

        [ForeignKey("RaisedById")]
        public ApplicationUser RaisedBy { get; set; } = null!;

        [Required]
        [StringLength(100)]
        public string Reason { get; set; } = string.Empty;

        [Required]
        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        public DisputeStatus Status { get; set; } = DisputeStatus.Open;

        public string? Resolution { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public string? ResolvedBy { get; set; } // Admin UserId

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
