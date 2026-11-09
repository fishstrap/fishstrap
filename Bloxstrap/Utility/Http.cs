namespace Bloxstrap.Utility
{
    internal static class Http
    {
        /// <summary>
        /// Gets and deserializes a JSON API response to the specified object
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="url"></param>
        /// <exception cref="HttpRequestException"></exception>
        /// <exception cref="JsonException"></exception>
        public static async Task<T> GetJson<T>(Uri url)
        {
            var request = await App.HttpClient.GetAsync(url);

            request.EnsureSuccessStatusCode();

            string json = await request.Content.ReadAsStringAsync();
            
            return JsonSerializer.Deserialize<T>(json)!;
        }

        public static async Task<T> SendJson<T>(HttpRequestMessage requestMessage)
        {
            var request = await App.HttpClient.SendAsync(requestMessage);

            request.EnsureSuccessStatusCode();

            string json = await request.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<T>(json)!;
        }

        public static async Task<T> AuthGetJson<T>(Uri url)
        {
            var request = await App.Cookies.AuthGet(url);

            request.EnsureSuccessStatusCode();

            string json = await request.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<T>(json)!;
        }

        public static async Task<T> AuthSendJson<T>(HttpRequestMessage requestMessage)
        {
            HttpContent content = requestMessage.Content!;

            var request = await App.Cookies.AuthPost(requestMessage.RequestUri, content);

            request.EnsureSuccessStatusCode();

            string json = await request.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<T>(json)!;
        }

        public static async Task<HttpRequestMessage> CloneHttpRequestMessageAsync(HttpRequestMessage request)
        {
            var clone = new HttpRequestMessage(request.Method, request.RequestUri)
            {
                Content = await CloneHttpContentAsync(request.Content),
                Version = request.Version
            };

            foreach (KeyValuePair<string, object?> prop in request.Options)
                clone.Options.TryAdd(prop.Key, prop.Value);

            foreach (KeyValuePair<string, IEnumerable<string>> header in request.Headers)
                clone.Headers.TryAddWithoutValidation(header.Key, header.Value);

            return clone;
        }

        public static async Task<HttpContent?> CloneHttpContentAsync(HttpContent? content)
        {
            if (content is null)
                return null;

            var memoryStream = new MemoryStream();
            await content.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            var clone = new StreamContent(memoryStream);
            foreach (var header in content.Headers)
                clone.Headers.TryAddWithoutValidation(header.Key, header.Value);

            return clone;
        }
    }
}
