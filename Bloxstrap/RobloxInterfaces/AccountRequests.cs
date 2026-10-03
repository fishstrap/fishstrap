namespace Bloxstrap.RobloxInterfaces
{
    internal static class AccountRequests
    {
        public static async Task SendAsync(HttpMethod method, Uri url, object body, string setting, string value)
        {
            using var response = await SendWithTokenAsync(method, url, body);

            await EnsureAcceptedAsync(response, setting, value);
        }

        public static async Task DeleteAsync(Uri url)
        {
            using var response = await SendWithTokenAsync(HttpMethod.Delete, url, null);

            response.EnsureSuccessStatusCode();
        }

        public static async Task<T> PostJsonAsync<T>(Uri url, object body)
        {
            using var response = await SendWithTokenAsync(HttpMethod.Post, url, body);

            response.EnsureSuccessStatusCode();

            return JsonSerializer.Deserialize<T>(await response.Content.ReadAsStringAsync())!;
        }

        private static async Task<HttpResponseMessage> SendWithTokenAsync(HttpMethod method, Uri url, object? body)
        {
            string? json = body is null ? null : JsonSerializer.Serialize(body);

            HttpRequestMessage Request() => new(method, url)
            {
                Content = json is null ? null : new StringContent(json, Encoding.UTF8, "application/json")
            };

            var first = await App.Cookies.AuthRequest(Request());

            if (first.StatusCode != HttpStatusCode.Forbidden || !first.Headers.TryGetValues("x-csrf-token", out var tokens))
                return first;

            first.Dispose();

            return await App.Cookies.AuthRequest(Request(), tokens.First());
        }

        private static async Task EnsureAcceptedAsync(HttpResponseMessage response, string setting, string value)
        {
            if (response.IsSuccessStatusCode)
                return;

            string body = await response.Content.ReadAsStringAsync();
            string? reason = null;

            try
            {
                using var document = JsonDocument.Parse(body);

                if (document.RootElement.ValueKind == JsonValueKind.Object
                    && document.RootElement.TryGetProperty("errors", out JsonElement errors)
                    && errors.ValueKind == JsonValueKind.Array && errors.GetArrayLength() > 0
                    && errors[0].ValueKind == JsonValueKind.Object
                    && errors[0].TryGetProperty("message", out JsonElement message)
                    && message.ValueKind == JsonValueKind.String)
                {
                    reason = message.GetString();
                }
            }
            catch (JsonException) { }

            if (String.IsNullOrWhiteSpace(reason))
                reason = String.IsNullOrWhiteSpace(body) ? null : body.Length > 200 ? body[..200] : body;

            throw new SettingRejectedException(setting, value, (int)response.StatusCode, reason);
        }
    }
}
