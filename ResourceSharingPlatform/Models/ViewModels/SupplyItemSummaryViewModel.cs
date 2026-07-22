namespace ResourceSharingPlatform.Models.ViewModels
{
    public class SupplyItemSummaryViewModel
    {
        public string ItemName { get; set; } = string.Empty;
        public int LocationCount { get; set; }
        public int TotalQuantity { get; set; }
        public string? Unit { get; set; }
        public bool HasLowStock { get; set; }
        public DateTime? NearestExpirationDate { get; set; }
    }
}
