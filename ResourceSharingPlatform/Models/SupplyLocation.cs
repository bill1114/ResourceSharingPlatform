namespace ResourceSharingPlatform.Models
{
    public class SupplyLocation
    {
        public int Id { get; set; }
        public string LocationName { get; set; } = string.Empty;
        public string? Address { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public string? ContactPerson { get; set; }
        public string? Phone { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        // Navigation property
        public List<SupplyItem> SupplyItems { get; set; } = new();
    }
}
