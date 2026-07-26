namespace ResourceSharingPlatform.Models
{
    public class SupplyDonationLog
    {
        public int Id { get; set; }
        public int SupplyItemId { get; set; }
        public int LocationId { get; set; }
        public int DonationQuantity { get; set; }
        public string DonorName { get; set; } = string.Empty;
        public string? DonorContact { get; set; }
        public string? Operator { get; set; }
        public DateTime DonationTime { get; set; } = DateTime.Now;
        public string? Remark { get; set; }

        // Navigation properties
        public SupplyItem? SupplyItem { get; set; }
        public SupplyLocation? Location { get; set; }
    }
}
