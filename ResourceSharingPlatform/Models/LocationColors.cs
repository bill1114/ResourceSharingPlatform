namespace ResourceSharingPlatform.Models
{
    public static class LocationColors
    {
        private static readonly (string Bg, string Text)[] Palette =
        {
            ("#0d6efd", "#fff"), // blue
            ("#6f42c1", "#fff"), // purple
            ("#d63384", "#fff"), // pink
            ("#fd7e14", "#000"), // orange
            ("#20c997", "#000"), // teal
            ("#6610f2", "#fff"), // indigo
            ("#198754", "#fff"), // green
            ("#0dcaf0", "#000"), // cyan
        };

        public static (string Bg, string Text) Get(int locationId)
        {
            var index = ((locationId - 1) % Palette.Length + Palette.Length) % Palette.Length;
            return Palette[index];
        }

        public static string Style(int locationId)
        {
            var (bg, text) = Get(locationId);
            return $"background-color:{bg};color:{text};";
        }
    }
}
