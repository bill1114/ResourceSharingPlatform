namespace ResourceSharingPlatform.Models
{
    public class SupplyDisposalLog
    {
        public int Id { get; set; }
        public int SupplyItemId { get; set; }
        public int LocationId { get; set; }
        public int DisposalQuantity { get; set; }
        public string Reason { get; set; } = DisposalReasons.Other;
        public string? Operator { get; set; }
        public DateTime DisposalTime { get; set; } = DateTime.Now;
        public string? Remark { get; set; }

        // Navigation properties
        public SupplyItem? SupplyItem { get; set; }
        public SupplyLocation? Location { get; set; }
    }
}
