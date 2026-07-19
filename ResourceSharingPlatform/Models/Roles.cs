namespace ResourceSharingPlatform.Models
{
    public static class Roles
    {
        public const string Admin = "Admin";
        public const string Cadre = "Cadre";
        public const string SocialWorker = "SocialWorker";

        public const string AdminAndCadre = "Admin,Cadre";

        public static readonly string[] All = { Admin, Cadre, SocialWorker };

        public static string ToDisplayName(string? roleName)
        {
            return roleName switch
            {
                Admin => "管理人員",
                Cadre => "幹部",
                SocialWorker => "社工",
                _ => roleName ?? string.Empty
            };
        }
    }
}
