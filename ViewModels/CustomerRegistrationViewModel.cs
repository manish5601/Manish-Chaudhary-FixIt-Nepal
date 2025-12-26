using System.ComponentModel.DataAnnotations;

namespace FixItNepal.ViewModels
{
    public class CustomerRegistrationViewModel
    {
        [Required]
        [Display(Name = "Full Name")]
        public string FullName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }

        [Required]
        public string Address { get; set; }

        [Required]
        [Phone]
        public string PhoneNumber { get; set; }

        [Display(Name = "Preferred Location")]
        public string? PreferredLocation { get; set; }

        [Display(Name = "Profile Picture")]
        public IFormFile? ProfileImage { get; set; }
    }
}
