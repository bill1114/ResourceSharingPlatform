namespace ResourceSharingPlatform.Models
{
    public class LineNotificationSettings
    {
        public int Id { get; set; }
        public bool IsEnabled { get; set; }
        public string? ChannelAccessToken { get; set; }
        public string? ChannelSecret { get; set; }
        public bool NotifyLowStock { get; set; } = true;
        public bool NotifyExpiringSoon { get; set; } = true;
        public bool NotifyExpired { get; set; } = true;
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
    }
}
