using FixItNepal.Models;
using System.ComponentModel.DataAnnotations;

namespace FixItNepal.ViewModels
{
    public class ProviderProfileViewModel
    {
        // User Info
        [Display(Name = "Full Name")]
        public string FullName { get; set; }

        [Phone]
        public string PhoneNumber { get; set; }

        public string Address { get; set; }

        public string? Email { get; set; } 

        public string? ProfilePicture { get; set; }

        [Display(Name = "Upload Profile Picture")]
        public IFormFile? ProfileImage { get; set; }

        // Service Info
        [Display(Name = "Primary Service")]
        public int ServiceCategoryId { get; set; }
        
        public string? ServiceCategoryName { get; set; }

        public IEnumerable<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>? Categories { get; set; }

        [Display(Name = "Years of Experience")]
        public int ExperienceYears { get; set; }

        public string? ServiceAreas { get; set; }
        
        public string? Skills { get; set; }

        public VerificationStatus Status { get; set; }
        
        // Read-only for now
        public List<ProviderDocument> Documents { get; set; } = new List<ProviderDocument>();
    }
}
