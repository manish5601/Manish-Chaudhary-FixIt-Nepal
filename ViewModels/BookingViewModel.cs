using System;

namespace FixItNepal.ViewModels
{
    public class BookingViewModel
    {
        public int ServiceItemId { get; set; }
        public string ServiceName { get; set; }
        public int ServiceProviderId { get; set; }
        public string ProviderName { get; set; }
        public DateTime BookingDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public decimal Price { get; set; }
        public string Notes { get; set; }
        public string CustomerAddress { get; set; }
        public string CustomerPhone { get; set; }
    }
}
