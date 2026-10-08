using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace CinemaManagement.MVC.Services
{
    public class ApiService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly JsonSerializerOptions _jsonOptions;

        public ApiService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        private HttpClient CreateClient()
        {
            return _httpClientFactory.CreateClient("CinemaAPI");
        }

        public async Task<T?> GetAsync<T>(string url)
        {
            var client = CreateClient();
            var response = await client.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                return default;

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(json, _jsonOptions);
        }

        public async Task<(bool Success, string Content)> GetRawAsync(string url)
        {
            var client = CreateClient();
            var response = await client.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();
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
                return (false, default, responseBody);

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

            return (response.IsSuccessStatusCode, responseBody);
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

            return (response.IsSuccessStatusCode, responseBody);
        }

        public async Task<(bool Success, string Message)> DeleteAsync(string url)
        {
            var client = CreateClient();
            var response = await client.DeleteAsync(url);
            var responseBody = await response.Content.ReadAsStringAsync();

            return (response.IsSuccessStatusCode, responseBody);
        }
    }
}