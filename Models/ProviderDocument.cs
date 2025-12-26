using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FixItNepal.Models
{
    public enum DocumentType
    {
        NationalId,
        CitizenshipCard,
        LicenseCertificate,
        SkillsCertificate,
        ExperienceLetter,
        Other
    }

    public class ProviderDocument
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ServiceProviderId { get; set; }

        [ForeignKey("ServiceProviderId")]
        public ServiceProvider ServiceProvider { get; set; } = null!;

        [Required]
        public DocumentType Type { get; set; }

        [Required]
        public string FilePath { get; set; } = string.Empty;

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        public bool IsVerified { get; set; } = false;
    }
}
