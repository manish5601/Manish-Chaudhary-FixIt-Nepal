using System.ComponentModel.DataAnnotations;

namespace FixItNepal.Models
{
    public class ServiceItem
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Service Name is required")]
        [Display(Name = "Service Name")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Description is required")]
        public string Description { get; set; }

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Price must be positive")]
        [DataType(DataType.Currency)]
        public decimal BasePrice { get; set; }

        [Required]
        public ServiceCategory Category { get; set; }

        public string? ImageUrl { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
