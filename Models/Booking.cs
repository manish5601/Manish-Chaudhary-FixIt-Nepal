using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FixItNepal.Models
{
    public enum BookingStatus
    {
        Pending,              // Awaiting provider response
        Confirmed,            // Provider accepted
        Completed,            // Service finished
        Cancelled,            // By customer or provider before confirmation
        Rejected,             // By provider
        AwaitingConfirmation,
        PaymentPending,       // Awaiting token payment
        Expired,              // No provider response within 24h
        RefundPending         // Marked for refund (optional for flow)
    }

    public class Booking
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CustomerId { get; set; }

        [ForeignKey("CustomerId")]
        public Customer Customer { get; set; } = null!;

        [Required]
        public int ServiceProviderId { get; set; }

        [ForeignKey("ServiceProviderId")]
        public ServiceProvider ServiceProvider { get; set; } = null!;

        [Required]
        public int ServiceItemId { get; set; }

        [ForeignKey("ServiceItemId")]
        public ServiceItem ServiceItem { get; set; } = null!;

        [Required]
        public DateTime BookingDate { get; set; }

        [Required]
        public TimeSpan StartTime { get; set; }

        [Required]
        public TimeSpan EndTime { get; set; }

        public BookingStatus Status { get; set; } = BookingStatus.Pending;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice { get; set; }

        // --- Payment & Lifecycle Fields ---
        [Column(TypeName = "decimal(18,2)")]
        public decimal TokenAmount { get; set; } = 10.00m;

        public bool IsTokenPaid { get; set; } = false;

        public string? ESewaTransactionId { get; set; }

        public DateTime? PaidAt { get; set; }
        
        // Expiration for provider response (24 hours after booking creation/payment)
        public DateTime? ExpiresAt { get; set; }

        // ---------------------------------

        public string? CustomerAddress { get; set; }
        public string? CustomerPhone { get; set; }

        public string? Notes { get; set; }
        
        public Review? Review { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
