using System.ComponentModel.DataAnnotations;

namespace ResourceSharingPlatform.Models.ViewModels
{
    public class AIStockInSettingsViewModel
    {
        public bool IsEnabled { get; set; }

        [StringLength(300)]
        public string? ApiEndpoint { get; set; }

        [StringLength(300)]
        public string? ApiKey { get; set; }

        public bool SupportsImageInput { get; set; } = true;
        public bool SupportsTextInput { get; set; } = true;
    }
}
