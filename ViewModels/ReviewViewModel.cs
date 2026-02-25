using System.ComponentModel.DataAnnotations;

namespace FixItNepal.ViewModels
{
    public class ReviewViewModel
    {
        [Required]
        public int BookingId { get; set; }

        public int ServiceProviderId { get; set; }
        public string? ServiceProviderName { get; set; }
        public string? ServiceName { get; set; }

        [Required]
        [Range(1, 5, ErrorMessage = "Please select a rating between 1 and 5.")]
        public int Rating { get; set; }

        [Required]
        [StringLength(500, ErrorMessage = "Comment cannot exceed 500 characters.")]
        public string Comment { get; set; } = string.Empty;
    }
}
