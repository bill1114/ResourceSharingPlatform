namespace ResourceSharingPlatform.Models
{
    public static class DisposalReasons
    {
        public const string Expired = "Expired";
        public const string Damaged = "Damaged";
        public const string Lost = "Lost";
        public const string Other = "Other";

        public static readonly string[] All = { Expired, Damaged, Lost, Other };

        public static string ToDisplayName(string? reason)
        {
            return reason switch
            {
                Expired => "過期",
                Damaged => "損壞",
                Lost => "遺失",
                Other => "其他",
                _ => reason ?? string.Empty
            };
        }

        public static string ToBadgeClass(string? reason)
        {
            return reason switch
            {
                Expired => "bg-danger",
                Damaged => "bg-warning text-dark",
                Lost => "bg-secondary",
                Other => "bg-dark",
                _ => "bg-secondary"
            };
        }
    }
}
