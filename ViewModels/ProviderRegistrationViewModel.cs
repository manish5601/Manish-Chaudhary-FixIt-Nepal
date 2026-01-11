using FixItNepal.Models;
using System.ComponentModel.DataAnnotations;

namespace FixItNepal.ViewModels
{
    public class ProviderRegistrationViewModel
    {
        // Basic Info
        [Required]
        [Display(Name = "Full Name")]
        public string FullName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 8)]
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

        // Professional Info
        [Required]
        [Display(Name = "Primary Service")]
        public ServiceCategory PrimaryService { get; set; }

        [Required]
        [Display(Name = "Years of Experience")]
        [Range(0, 50)]
        public int ExperienceYears { get; set; }

        [Display(Name = "Service Areas (Comma separated)")]
        public string ServiceAreas { get; set; }

        public string? Skills { get; set; }

        // Document Upload
        [Required(ErrorMessage = "Please upload your Citizenship or National ID")]
        [Display(Name = "Identification Document (PDF/Image)")]
        public IFormFile IdentificationDocument { get; set; }

        [Display(Name = "Profile Picture")]
        public IFormFile? ProfileImage { get; set; }
    }
}
