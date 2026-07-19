namespace ResourceSharingPlatform.Models.ViewModels
{
    public class DashboardViewModel
    {
        public int TotalLocationCount { get; set; }
        public int TotalItemTypeCount { get; set; }
        public int TotalQuantity { get; set; }
        public int LowStockCount { get; set; }
        public int ExpiringSoonCount { get; set; }
        public int ExpiredCount { get; set; }

        public List<SupplyItem> LowStockItems { get; set; } = new();
        public List<SupplyItem> ExpiringSoonItems { get; set; } = new();
        public List<LocationSummaryItem> LocationSummaries { get; set; } = new();
    }

    public class LocationSummaryItem
    {
        public int LocationId { get; set; }
        public string LocationName { get; set; } = string.Empty;
        public int ItemTypeCount { get; set; }
        public int TotalQuantity { get; set; }
        public int LowStockCount { get; set; }
    }
}
