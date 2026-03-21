using FixItNepal.Models;

namespace FixItNepal.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int ActiveProviders { get; set; }
        public int BookingsToday { get; set; }
        public int PendingProviders { get; set; }

        // Booking Stats
        public int TotalBookings { get; set; }
        public int CompletedBookings { get; set; }
        public int CancelledBookings { get; set; }
        public int PendingBookings { get; set; }

        // Financial Stats
        public decimal TotalRevenue { get; set; }

        // Insights
        public List<CategoryInsight> TopCategories { get; set; } = new List<CategoryInsight>();
        public List<ProviderInsight> TopProviders { get; set; } = new List<ProviderInsight>();

        public List<DashboardActivity> RecentActivities { get; set; } = new List<DashboardActivity>();
    }

    public class CategoryInsight
    {
        public string CategoryName { get; set; }
        public int BookingCount { get; set; }
        public decimal Revenue { get; set; }
    }

    public class ProviderInsight
    {
        public string ProviderName { get; set; }
        public decimal AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public int CompletedBookings { get; set; }
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
