using FixItNepal.Models;
using System.ComponentModel.DataAnnotations;

namespace FixItNepal.ViewModels
{
    public class ServiceItemViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Service Name is required")]
        [Display(Name = "Service Name")]
        public string Name { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Price must be positive")]
        [DataType(DataType.Currency)]
        public decimal BasePrice { get; set; }

        [Required]
        public ServiceCategory Category { get; set; }

        [Display(Name = "Service Image")]
        public IFormFile? ImageFile { get; set; }

        public string? CurrentImageUrl { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
