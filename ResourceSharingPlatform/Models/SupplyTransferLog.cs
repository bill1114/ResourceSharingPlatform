namespace ResourceSharingPlatform.Models
{
    public class SupplyTransferLog
    {
        public int Id { get; set; }
        public Guid BatchId { get; set; }
        public int SupplyItemId { get; set; }
        public int FromLocationId { get; set; }
        public int ToLocationId { get; set; }
        public int TransferQuantity { get; set; }
        public DateTime TransferTime { get; set; } = DateTime.Now;
        public string Status { get; set; } = TransferStatuses.Pending;
        public string? ConfirmedBy { get; set; }
        public DateTime? ConfirmedAt { get; set; }
        public string? Operator { get; set; }
        public string? Remark { get; set; }

        // Navigation properties
        public SupplyItem? SupplyItem { get; set; }
        public SupplyLocation? FromLocation { get; set; }
        public SupplyLocation? ToLocation { get; set; }
    }
}
