namespace ResourceSharingPlatform.Models
{
    public class SupplyItem
    {
        public int Id { get; set; }
        public string Category { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string? Specification { get; set; }
        public int Quantity { get; set; }
        public string? Unit { get; set; }
        public string StockType { get; set; } = StockTypes.HasExpiry;
        public DateTime? ExpirationDate { get; set; }
        public string? ImagePath { get; set; }
        public int? InventoryItemVariantId { get; set; }
        public int LocationId { get; set; }
        public int SafetyStock { get; set; }
        public string? Remark { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        // Navigation property
        public SupplyLocation? Location { get; set; }
        public InventoryItemVariant? InventoryItemVariant { get; set; }

        // Helper method to get status
        public string GetStatus()
        {
            if (Quantity <= SafetyStock)
            {
                return "低庫存";
            }
            else if (ExpirationDate != null && ExpirationDate < DateTime.Today)
            {
                return "已過期";
            }
            else if (ExpirationDate != null && ExpirationDate <= DateTime.Today.AddDays(30))
            {
                return "即將過期";
            }
            else
            {
                return "正常";
            }
        }

        // Helper method to get status badge class
        public string GetStatusBadgeClass()
        {
            var status = GetStatus();
            return status switch
            {
                "低庫存" => "bg-danger",
                "已過期" => "bg-dark",
                "即將過期" => "bg-warning text-dark",
                _ => "bg-success"
            };
        }
    }
}
