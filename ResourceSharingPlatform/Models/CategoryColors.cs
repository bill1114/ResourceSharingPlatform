namespace ResourceSharingPlatform.Models
{
    public static class CategoryColors
    {
        private static readonly (string Bg, string Text)[] Palette =
        {
            ("#ffb703", "#000"), // amber
            ("#8ecae6", "#000"), // sky blue
            ("#ff8fa3", "#000"), // rose
            ("#06d6a0", "#000"), // mint
            ("#9d4edd", "#fff"), // violet
            ("#f4a261", "#000"), // orange
            ("#457b9d", "#fff"), // slate blue
            ("#c9184a", "#fff"), // crimson
        };

        // Category is free-text (no stable Id), so pick a color from a hash of the
        // string itself. string.GetHashCode() is randomized per-process in .NET,
        // so a small stable hash is used here to keep colors consistent across restarts.
        private static int StableHash(string value)
        {
            unchecked
            {
                var hash = 17;
                foreach (var c in value)
                {
                    hash = hash * 31 + c;
                }
                return hash;
            }
        }

        public static (string Bg, string Text) Get(string? category)
        {
            if (string.IsNullOrEmpty(category))
            {
                return Palette[0];
            }

            var index = ((StableHash(category) % Palette.Length) + Palette.Length) % Palette.Length;
            return Palette[index];
        }

        public static string Style(string? category)
        {
            var (bg, text) = Get(category);
            return $"background-color:{bg};color:{text};";
        }
    }
}
