using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace FixItNepal.ViewModels
{
    public class ServiceRequestViewModel
    {
        [Required(ErrorMessage = "Please provide a title for your request")]
        [StringLength(100)]
        [Display(Name = "Task Title (e.g., Fix Leaking Tap)")]
        public string Title { get; set; } = null!;

        [Required(ErrorMessage = "Please describe the problem in detail")]
        [Display(Name = "Problem Description")]
        public string Description { get; set; } = null!;

        [Required(ErrorMessage = "Please select a category")]
        [Display(Name = "Service Category")]
        public int ServiceCategoryId { get; set; }

        public IEnumerable<SelectListItem>? Categories { get; set; }

        [Display(Name = "Service Address")]
        public string? Address { get; set; }

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        [Display(Name = "Task Photo (Optional)")]
        public IFormFile? ImageFile { get; set; }
        
        public string? ExistingImageUrl { get; set; }
    }

    public class ServiceBidViewModel
    {
        public int ServiceRequestId { get; set; }
        
        [Required]
        [Range(1, 100000)]
        [Display(Name = "Proposed Price (NPR)")]
        public decimal ProposedPrice { get; set; }

        [Required]
        [Display(Name = "Estimated Time to Complete")]
        public string EstimatedTime { get; set; } = null!;

        [Required]
        [Display(Name = "Message to Customer")]
        public string Notes { get; set; } = null!;
    }
}
