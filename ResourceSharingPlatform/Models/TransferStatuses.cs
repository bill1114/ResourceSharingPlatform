namespace ResourceSharingPlatform.Models
{
    public static class TransferStatuses
    {
        public const string Pending = "Pending";
        public const string Confirmed = "Confirmed";
        public const string Cancelled = "Cancelled";

        public static string ToDisplayName(string? status)
        {
            return status switch
            {
                Pending => "待確認",
                Confirmed => "已確認",
                Cancelled => "已取消",
                _ => status ?? string.Empty
            };
        }

        public static string ToBadgeClass(string? status)
        {
            return status switch
            {
                Pending => "bg-warning text-dark",
                Confirmed => "bg-success",
                Cancelled => "bg-secondary",
                _ => "bg-light text-dark"
            };
        }
    }
}
