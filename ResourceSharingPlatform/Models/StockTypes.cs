namespace ResourceSharingPlatform.Models
{
    public static class StockTypes
    {
        public const string NoExpiry = "NoExpiry";
        public const string HasExpiry = "HasExpiry";
        public const string Frozen = "Frozen";

        public static readonly string[] All = { NoExpiry, HasExpiry, Frozen };

        public static string ToDisplayName(string? stockType)
        {
            return stockType switch
            {
                NoExpiry => "無效期物資",
                HasExpiry => "有效期物資",
                Frozen => "冷凍食品",
                _ => stockType ?? string.Empty
            };
        }

        public static string ToBadgeClass(string? stockType)
        {
            return stockType switch
            {
                NoExpiry => "bg-primary",
                HasExpiry => "bg-success",
                Frozen => "bg-info text-dark",
                _ => "bg-secondary"
            };
        }
    }
}
