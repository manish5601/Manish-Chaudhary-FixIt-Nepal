using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FixItNepal.Models
{
    public enum ServiceRequestStatus
    {
        Open,           // Newly posted, accepting bids
        Bidding,        // Bids received
        Accepted,       // Customer accepted a bid, waiting for payment/confirmation
        InProgress,     // Job is being worked on
        Completed,      // Job finished
        Cancelled       // By customer
    }

    public class ServiceRequest
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CustomerId { get; set; }

        [ForeignKey("CustomerId")]
        public Customer Customer { get; set; } = null!;

        [Required]
        public int ServiceCategoryId { get; set; }

        [ForeignKey("ServiceCategoryId")]
        public ServiceCategory ServiceCategory { get; set; } = null!;

        [Required]
        [StringLength(100)]
        public string Title { get; set; } = null!;

        [Required]
        public string Description { get; set; } = null!;

        public string? Address { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string? ImageUrl { get; set; }

        public ServiceRequestStatus Status { get; set; } = ServiceRequestStatus.Open;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property for bids
        public ICollection<ServiceBid> Bids { get; set; } = new List<ServiceBid>();
    }
}
