namespace ResourceSharingPlatform.Models.ViewModels
{
    public class MapLocationViewModel
    {
        public int LocationId { get; set; }
        public string LocationName { get; set; } = string.Empty;
        public string? Address { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public int ItemTypeCount { get; set; }
        public int TotalQuantity { get; set; }
        public int LowStockCount { get; set; }
        public int ExpiringSoonCount { get; set; }
    }
}
