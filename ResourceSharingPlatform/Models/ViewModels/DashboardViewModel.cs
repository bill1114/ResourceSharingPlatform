namespace ResourceSharingPlatform.Models.ViewModels
{
    public class DashboardViewModel
    {
        public int TotalLocationCount { get; set; }
        public int TotalItemTypeCount { get; set; }
        public int TotalQuantity { get; set; }
        public int LowStockCount { get; set; }
        public int GlobalLowStockCount { get; set; }
        public int ExpiringSoonCount { get; set; }
        public int ExpiredCount { get; set; }

        public List<LocationLowStockItem> LocationLowStockItems { get; set; } = new();
        public List<GlobalLowStockItem> GlobalLowStockItems { get; set; } = new();
        public List<SupplyItem> ExpiringSoonItems { get; set; } = new();
        public List<LocationSummaryItem> LocationSummaries { get; set; } = new();
    }

    public class LocationLowStockItem
    {
        public int LocationId { get; set; }
        public string LocationName { get; set; } = string.Empty;
        public int DefinitionId { get; set; }
        public string Category { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public int TotalQuantity { get; set; }
        public int SafetyStock { get; set; }
        public int Shortage => Math.Max(0, SafetyStock - TotalQuantity);
    }

    public class GlobalLowStockItem
    {
        public int DefinitionId { get; set; }
        public string Category { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public int StoredQuantity { get; set; }
        public int TotalQuantity => StoredQuantity;
        public int SafetyStock { get; set; }
        public int Shortage => Math.Max(0, SafetyStock - TotalQuantity);
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
