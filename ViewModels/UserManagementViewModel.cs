namespace FixItNepal.ViewModels
{
    public class UserManagementViewModel
    {
        public string Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Role { get; set; }
        public bool IsActive { get; set; }
        public string? ProfilePicture { get; set; }
        public string? ProviderStatus { get; set; }
    }
}
