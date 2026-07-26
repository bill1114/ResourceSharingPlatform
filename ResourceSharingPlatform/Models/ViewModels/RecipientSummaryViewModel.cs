namespace ResourceSharingPlatform.Models.ViewModels
{
    public class RecipientSummaryViewModel
    {
        public string RecipientName { get; set; } = string.Empty;
        public string? RecipientContact { get; set; }
        public int PickupCount { get; set; }
        public bool IsFrequent { get; set; }
        public string ItemBreakdown { get; set; } = string.Empty;
        public DateTime FirstPickupDate { get; set; }
        public DateTime LastPickupDate { get; set; }
    }
}
