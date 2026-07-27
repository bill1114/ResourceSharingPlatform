namespace ResourceSharingPlatform.Models
{
    // Single-row configuration for the external AI model used by AI 智慧入庫.
    // Reserved now with no active caller; ApiEndpoint/ApiKey stay blank until
    // the actual model/API is wired up.
    public class AIStockInSettings
    {
        public int Id { get; set; }
        public bool IsEnabled { get; set; }
        public string? ApiEndpoint { get; set; }
        public string? ApiKey { get; set; }
        public bool SupportsImageInput { get; set; } = true;
        public bool SupportsTextInput { get; set; } = true;
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
    }
}
