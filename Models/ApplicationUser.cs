using Microsoft.AspNetCore.Identity;

namespace FixItNepal.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? FullName { get; set; }
        public string? Address { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;

        public string? ProfilePicture { get; set; }

        // Navigation properties
        public Customer? Customer { get; set; }
        public FixItNepal.Models.ServiceProvider? ServiceProvider { get; set; }
    }
}
