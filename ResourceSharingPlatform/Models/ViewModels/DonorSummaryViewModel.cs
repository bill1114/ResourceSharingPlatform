namespace ResourceSharingPlatform.Models.ViewModels
{
    public class DonorSummaryViewModel
    {
        public string DonorName { get; set; } = string.Empty;
        public string? DonorContact { get; set; }
        public int DonationCount { get; set; }
        public int DistinctItemCount { get; set; }
        public DateTime FirstDonationDate { get; set; }
        public DateTime LastDonationDate { get; set; }
    }
}
