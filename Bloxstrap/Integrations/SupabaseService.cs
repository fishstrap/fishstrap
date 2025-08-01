using System.Net.Http.Headers;
using System.Web;

namespace Bloxstrap.Integrations
{
    public class SupabaseFlaglist
    {
        public Guid id { get; set; }
        public string? title { get; set; }
        public string? name { get; set; }
        public JsonElement json { get; set; }
        public DateTime created_at { get; set; }
        public DateTime? updated_at { get; set; }
        public string? user_id { get; set; }
    }

    public class SupabaseService : IDisposable
    {
        private readonly HttpClient _client;
        private readonly string _baseUrl;
        private readonly string _apiKey;
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };
        private bool _disposed;

        public SupabaseService(string baseUrl, string apiKey, HttpClient? httpClient = null)
        {
            _baseUrl = baseUrl.TrimEnd('/');
            _apiKey = apiKey;

            _client = httpClient ?? new HttpClient();
            _client.BaseAddress = new Uri(_baseUrl);

            if (!_client.DefaultRequestHeaders.Contains("Authorization"))
                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

            if (!_client.DefaultRequestHeaders.Contains("apikey"))
                _client.DefaultRequestHeaders.Add("apikey", _apiKey);

            if (!_client.DefaultRequestHeaders.Accept.Contains(new MediaTypeWithQualityHeaderValue("application/json")))
                _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task<bool> UploadFlaglistAsync(string title, string json, string userId, string? name = null)
        {
            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("Title cannot be null or whitespace.", nameof(title));
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentException("JSON cannot be null or whitespace.", nameof(json));
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("UserId cannot be null or whitespace.", nameof(userId));

            JsonElement jsonElement;
            try
            {
                using var doc = JsonDocument.Parse(json);
                jsonElement = doc.RootElement.Clone();
            }
            catch (JsonException ex)
            {
                throw new ArgumentException("Invalid JSON format.", nameof(json), ex);
            }

            var payload = new
            {
                title,
                name,
                json = jsonElement,
                user_id = userId
            };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await _client.PostAsync("/rest/v1/flaglists", content).ConfigureAwait(false);

            return response.IsSuccessStatusCode;
        }

        public async Task<List<SupabaseFlaglist>> GetFlaglistsAsync()
        {
            var response = await _client.GetAsync("/rest/v1/flaglists?select=*").ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
                return new List<SupabaseFlaglist>();

            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            return JsonSerializer.Deserialize<List<SupabaseFlaglist>>(content, _jsonOptions) ?? new List<SupabaseFlaglist>();
        }

        public async Task<bool> DeleteFlaglistAsync(Guid id, string userId)
        {
            if (userId == null) throw new ArgumentNullException(nameof(userId));

            var encodedId = HttpUtility.UrlEncode(id.ToString());
            var encodedUserId = HttpUtility.UrlEncode(userId);

            var response = await _client.DeleteAsync($"/rest/v1/flaglists?id=eq.{encodedId}&user_id=eq.{encodedUserId}").ConfigureAwait(false);

            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateFlaglistAsync(Guid id, string title, string json, string userId, string? name = null)
        {
            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("Title cannot be null or whitespace.", nameof(title));
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentException("JSON cannot be null or whitespace.", nameof(json));
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("UserId cannot be null or whitespace.", nameof(userId));

            JsonElement jsonElement;
            try
            {
                using var doc = JsonDocument.Parse(json);
                jsonElement = doc.RootElement.Clone();
            }
            catch (JsonException ex)
            {
                throw new ArgumentException("Invalid JSON format.", nameof(json), ex);
            }

            var payload = new
            {
                title,
                name,
                json = jsonElement,
            };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var url = $"/rest/v1/flaglists?id=eq.{id}&user_id=eq.{userId}";

            var request = new HttpRequestMessage(HttpMethod.Patch, url)
            {
                Content = content
            };

            var response = await _client.SendAsync(request).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Supabase update failed: {response.StatusCode} - {errorContent}");
            }

            return true;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _client.Dispose();
                _disposed = true;
            }
        }
    }
}