namespace ResourceSharingPlatform.Services
{
    // Resolves where uploaded files (item photos, AI stock-in photos) live on disk.
    //
    // This app currently runs as two separate deployments on the same machine - the dev
    // copy launched by Start.bat (Kestrel, straight from the source tree) and the IIS site
    // (its own "dotnet publish" output folder). Each has its own wwwroot, so storing uploads
    // under wwwroot made them diverge: a photo uploaded on one side was invisible on the
    // other, and a republish could never carry them over.
    //
    // Configuring "UploadsRoot" (an absolute path) in appsettings.json points both
    // deployments at the same folder outside of either wwwroot, so uploads are shared and
    // survive republishing. Leaving it unset falls back to the original wwwroot/uploads
    // behavior, so this stays safe on a fresh checkout that hasn't set it up.
    public class UploadPathProvider
    {
        public UploadPathProvider(IConfiguration configuration, IWebHostEnvironment environment)
        {
            var configured = configuration["UploadsRoot"];
            Root = string.IsNullOrWhiteSpace(configured)
                ? Path.Combine(environment.WebRootPath, "uploads")
                : configured;

            Directory.CreateDirectory(Root);
        }

        public string Root { get; }

        public string GetSubfolder(string name)
        {
            var path = Path.Combine(Root, name);
            Directory.CreateDirectory(path);
            return path;
        }

        // Resolves a stored relative path like "/uploads/items/xxx.png" back to its
        // physical location under Root, for delete-on-replace/delete-on-remove logic.
        public string ResolveRelativePath(string relativePath)
        {
            var trimmed = relativePath.TrimStart('/');
            const string prefix = "uploads/";
            if (trimmed.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                trimmed = trimmed[prefix.Length..];
            }

            return Path.Combine(Root, trimmed.Replace('/', Path.DirectorySeparatorChar));
        }
    }
}
