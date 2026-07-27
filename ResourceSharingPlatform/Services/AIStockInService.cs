using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using ResourceSharingPlatform.Data;
using ResourceSharingPlatform.Models;

namespace ResourceSharingPlatform.Services
{
    public class AIStockInService
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        private static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
        private const long MaxImageSizeBytes = 5 * 1024 * 1024;

        public AIStockInService(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        public bool TryValidateImage(IFormFile file, out string? error)
        {
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedImageExtensions.Contains(ext))
            {
                error = "圖片格式僅支援 jpg、jpeg、png、webp";
                return false;
            }

            if (file.Length > MaxImageSizeBytes)
            {
                error = "圖片大小不可超過 5MB";
                return false;
            }

            error = null;
            return true;
        }

        public async Task<AIStockInLog> RecognizeAsync(int locationId, string inputType, string? inputText, IFormFile? image, string? operatorName)
        {
            var settings = await _context.AIStockInSettings.FirstOrDefaultAsync();

            string? imagePath = null;
            if (image != null && image.Length > 0)
            {
                imagePath = await SaveImageAsync(image);
            }

            var log = new AIStockInLog
            {
                LocationId = locationId,
                InputType = inputType,
                InputText = inputText,
                InputImagePath = imagePath,
                Operator = operatorName,
                CreatedAt = DateTime.Now
            };

            // 尚未取得外部開源模型 API 的端點/驗證/輸入輸出格式細節，故此處先只記錄輸入內容，
            // 不產生辨識建議。之後補上真實 API 規格後，改成在這裡呼叫 settings.ApiEndpoint
            // 並把回應解析進 Suggested* 欄位即可，不需要更動資料庫結構或確認流程。
            if (settings == null || !settings.IsEnabled || string.IsNullOrWhiteSpace(settings.ApiEndpoint))
            {
                log.RawResponse = "AI 智慧入庫尚未啟用或未設定 API，請聯絡管理人員於「系統管理 > AI 智慧入庫設定」中開啟並填入模型端點。";
            }
            else
            {
                log.RawResponse = "（尚未串接真實 AI 模型 API）已記錄本次輸入內容，請於下方手動確認正確的物資資訊。";
            }

            _context.AIStockInLogs.Add(log);
            await _context.SaveChangesAsync();

            return log;
        }

        private async Task<string> SaveImageAsync(IFormFile file)
        {
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "ai-stockin");
            Directory.CreateDirectory(uploadsFolder);

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var fileName = Guid.NewGuid().ToString("N") + ext;
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return "/uploads/ai-stockin/" + fileName;
        }
    }
}
