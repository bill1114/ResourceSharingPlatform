using System.ComponentModel.DataAnnotations;

namespace ResourceSharingPlatform.Models.ViewModels
{
    public class LineNotificationSettingsViewModel
    {
        public bool IsEnabled { get; set; }

        [StringLength(300)]
        public string? ChannelAccessToken { get; set; }

        [StringLength(300)]
        public string? ChannelSecret { get; set; }

        public bool NotifyLowStock { get; set; } = true;
        public bool NotifyExpiringSoon { get; set; } = true;
        public bool NotifyExpired { get; set; } = true;
    }
}
