using System.ComponentModel.DataAnnotations;

namespace FixItNepal.Models
{
    public class ServiceCategory
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Category Name is required")]
        [Display(Name = "Category Name")]
        public string Name { get; set; }

        public string? Description { get; set; }

        public string? IconPath { get; set; }

        public bool IsActive { get; set; } = true;
        
        public ICollection<ServiceItem>? ServiceItems { get; set; }
    }
}
