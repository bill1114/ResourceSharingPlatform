namespace ResourceSharingPlatform.Models
{
    public class SupplyOutboundLog
    {
        public int Id { get; set; }
        public int SupplyItemId { get; set; }
        public int LocationId { get; set; }
        public int OutboundQuantity { get; set; }
        public string RecipientName { get; set; } = string.Empty;
        public string? RecipientContact { get; set; }
        public string? Operator { get; set; }
        public DateTime OutboundTime { get; set; } = DateTime.Now;
        public string? Remark { get; set; }

        // Navigation properties
        public SupplyItem? SupplyItem { get; set; }
        public SupplyLocation? Location { get; set; }
    }
}
