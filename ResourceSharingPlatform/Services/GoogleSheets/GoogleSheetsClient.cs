using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace ResourceSharingPlatform.Services.GoogleSheets
{
    public class SheetsResponse<T>
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public T? Data { get; set; }
    }

    public class SheetsActionResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
    }

    // Thin wrapper around the Google Apps Script Web App. All requests are POSTed
    // as { action, secret, payload }. Apps Script web apps respond via a 302 to a
    // googleusercontent.com content URL; HttpClient's default auto-redirect
    // (which downgrades the redirected request to GET) is what Google expects here
    // - it is just fetching the already-computed response body, not re-running the
    // script - so no custom redirect handling is needed.
    public class GoogleSheetsClient
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly HttpClient _http;
        private readonly GoogleSheetsOptions _options;

        public GoogleSheetsClient(HttpClient http, IOptions<GoogleSheetsOptions> options)
        {
            _http = http;
            _options = options.Value;
        }

        public async Task<SheetsResponse<T>> PostAsync<T>(string action, object? payload = null)
        {
            var result = await SendAsync<SheetsResponse<T>>(action, payload);
            return result ?? new SheetsResponse<T> { Success = false, Message = "GAS 沒有回應內容" };
        }

        public async Task<SheetsActionResult> PostActionAsync(string action, object? payload = null)
        {
            var result = await SendAsync<SheetsActionResult>(action, payload);
            return result ?? new SheetsActionResult { Success = false, Message = "GAS 沒有回應內容" };
        }

        private async Task<T?> SendAsync<T>(string action, object? payload)
        {
            if (string.IsNullOrWhiteSpace(_options.WebAppUrl))
            {
                throw new InvalidOperationException("尚未設定 GoogleSheets:WebAppUrl，請參考 GoogleAppsScript/SETUP.md 設定。");
            }

            var body = new { action, secret = _options.ApiSecret, payload };
            using var response = await _http.PostAsJsonAsync(_options.WebAppUrl, body, JsonOptions);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<T>(JsonOptions);
        }
    }
}
