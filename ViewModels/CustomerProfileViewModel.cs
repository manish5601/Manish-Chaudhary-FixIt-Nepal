using System.ComponentModel.DataAnnotations;

namespace FixItNepal.ViewModels
{
    public class CustomerProfileViewModel
    {
        [Display(Name = "Full Name")]
        [Required]
        public string FullName { get; set; }

        [EmailAddress]
        public string? Email { get; set; }

        [Phone]
        [Required]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }

        [Required]
        public string Address { get; set; }

        [Display(Name = "Preferred Location")]
        public string? PreferredLocation { get; set; }

        [Display(Name = "Profile Picture")]
        public string? ProfilePicture { get; set; }
    }
}
