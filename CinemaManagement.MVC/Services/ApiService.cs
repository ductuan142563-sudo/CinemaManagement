using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace CinemaManagement.MVC.Services
{
    public class ApiService
    {
        private const string TokenKey = "Token";

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly JsonSerializerOptions _jsonOptions;

        public ApiService(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor)
        {
            _httpClientFactory = httpClientFactory;
            _httpContextAccessor = httpContextAccessor;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        private HttpClient CreateClient()
        {
            var client = _httpClientFactory.CreateClient("CinemaAPI");

            // Gắn token (nếu đã đăng nhập) vào mọi request gọi API
            var token = _httpContextAccessor.HttpContext?.Session.GetString(TokenKey);
            if (!string.IsNullOrEmpty(token))
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            return client;
        }

        // Token hết hạn/không hợp lệ -> xóa Session để lần chuyển trang sau bị đưa về Login
        private void HandleUnauthorized(HttpResponseMessage response)
        {
            if (response.StatusCode != HttpStatusCode.Unauthorized)
                return;

            var session = _httpContextAccessor.HttpContext?.Session;
            if (session != null && !string.IsNullOrEmpty(session.GetString(TokenKey)))
                session.Clear();
        }

        // Lấy riêng nội dung "message" từ body lỗi của API; nếu không có thì trả nguyên body
        private static string ExtractMessage(HttpResponseMessage response, string body)
        {
            if (response.StatusCode == HttpStatusCode.Forbidden)
                return "Bạn không có quyền thực hiện thao tác này.";

            if (response.StatusCode == HttpStatusCode.Unauthorized && string.IsNullOrWhiteSpace(body))
                return "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.";

            try
            {
                using var doc = JsonDocument.Parse(body);
                if (doc.RootElement.ValueKind == JsonValueKind.Object)
                {
                    foreach (var prop in doc.RootElement.EnumerateObject())
                    {
                        if (prop.Name.Equals("message", StringComparison.OrdinalIgnoreCase)
                            && prop.Value.ValueKind == JsonValueKind.String)
                        {
                            return prop.Value.GetString() ?? body;
                        }
                    }
                }
            }
            catch (JsonException)
            {
                // body không phải JSON -> trả nguyên văn
            }

            return body;
        }

        public async Task<T?> GetAsync<T>(string url)
        {
            var client = CreateClient();
            var response = await client.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                HandleUnauthorized(response);
                return default;
            }

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(json, _jsonOptions);
        }

        public async Task<(bool Success, string Content)> GetRawAsync(string url)
        {
            var client = CreateClient();
            var response = await client.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                HandleUnauthorized(response);

            return (response.IsSuccessStatusCode, content);
        }

        public async Task<(bool Success, T? Data, string Message)> PostAsync<T>(string url, object data)
        {
            var client = CreateClient();
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(url, content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                HandleUnauthorized(response);
                return (false, default, ExtractMessage(response, responseBody));
            }

            var result = JsonSerializer.Deserialize<T>(responseBody, _jsonOptions);
            return (true, result, responseBody);
        }

        public async Task<(bool Success, string Message)> PostAsync(string url, object data)
        {
            var client = CreateClient();
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(url, content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                HandleUnauthorized(response);
                return (false, ExtractMessage(response, responseBody));
            }

            // Thành công: giữ nguyên body (AccountController đọc JSON đăng nhập từ đây)
            return (true, responseBody);
        }

        public async Task<(bool Success, string Message)> PutAsync(string url, object? data = null)
        {
            var client = CreateClient();
            StringContent? content = null;

            if (data != null)
            {
                var json = JsonSerializer.Serialize(data);
                content = new StringContent(json, Encoding.UTF8, "application/json");
            }

            var response = await client.PutAsync(url, content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                HandleUnauthorized(response);
                return (false, ExtractMessage(response, responseBody));
            }

            return (true, responseBody);
        }

        public async Task<(bool Success, string Message)> DeleteAsync(string url)
        {
            var client = CreateClient();
            var response = await client.DeleteAsync(url);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                HandleUnauthorized(response);
                return (false, ExtractMessage(response, responseBody));
            }

            return (true, responseBody);
        }
    }
}