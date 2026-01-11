using FixItNepal.Models;

namespace FixItNepal.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int ActiveProviders { get; set; }
        public int BookingsToday { get; set; }
        public int PendingProviders { get; set; } // Extra stat usually helpful

        public List<DashboardActivity> RecentActivities { get; set; } = new List<DashboardActivity>();
    }

    public class DashboardActivity
    {
        public string UserName { get; set; }
        public string Action { get; set; }
        public string TimeAgo { get; set; }
        public string StatusColor { get; set; } // bg-success-subtle text-success, etc.
        public string StatusText { get; set; }
    }
}
