using System.ComponentModel.DataAnnotations;

namespace FixItNepal.ViewModels
{
    public class DisputeViewModel
    {
        [Required]
        public int BookingId { get; set; }

        public string? ServiceName { get; set; }
        public string? PartyName { get; set; } // The name of the other party (Provider or Customer)

        [Required]
        [StringLength(100, ErrorMessage = "Reason cannot exceed 100 characters.")]
        public string Reason { get; set; } = string.Empty;

        [Required]
        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
        public string Description { get; set; } = string.Empty;
    }
}
