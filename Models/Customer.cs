using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FixItNepal.Models
{
    public class Customer
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; } = null!;

        public string? PreferredLocation { get; set; }
        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

        // Navigation
        // public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    }
}
