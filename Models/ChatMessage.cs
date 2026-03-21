using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FixItNepal.Models
{
    public class ChatMessage
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string SenderId { get; set; } = string.Empty;

        [ForeignKey("SenderId")]
        public ApplicationUser Sender { get; set; } = null!;

        [Required]
        public string ReceiverId { get; set; } = string.Empty;

        [ForeignKey("ReceiverId")]
        public ApplicationUser Receiver { get; set; } = null!;

        [Required]
        public string Message { get; set; } = string.Empty;

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public bool IsRead { get; set; } = false;

        public int? BookingId { get; set; }

        [ForeignKey("BookingId")]
        public Booking? Booking { get; set; }
    }
}
