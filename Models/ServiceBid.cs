using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FixItNepal.Models
{
    public enum BidStatus
    {
        Pending,
        Accepted,
        Rejected,
        Cancelled
    }

    public class ServiceBid
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ServiceRequestId { get; set; }

        [ForeignKey("ServiceRequestId")]
        public ServiceRequest ServiceRequest { get; set; } = null!;

        [Required]
        public int ServiceProviderId { get; set; }

        [ForeignKey("ServiceProviderId")]
        public ServiceProvider ServiceProvider { get; set; } = null!;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal ProposedPrice { get; set; }

        public string? EstimatedTime { get; set; }

        [Required]
        public string Notes { get; set; } = null!;

        public BidStatus Status { get; set; } = BidStatus.Pending;

        // Counter-Bargaining Fields
        [Column(TypeName = "decimal(18,2)")]
        public decimal? CustomerCounterPrice { get; set; }
        
        public string? CustomerCounterMessage { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
