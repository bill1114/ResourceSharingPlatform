namespace ResourceSharingPlatform.Models
{
    public static class AIStockInInputTypes
    {
        public const string Image = "Image";
        public const string Text = "Text";

        public static readonly string[] All = { Image, Text };

        public static string ToDisplayName(string? inputType)
        {
            return inputType switch
            {
                Image => "照片辨識",
                Text => "文字描述",
                _ => inputType ?? string.Empty
            };
        }
    }
}
