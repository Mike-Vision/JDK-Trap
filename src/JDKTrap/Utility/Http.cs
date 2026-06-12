using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace JDKTrap.Utility
{
    internal static class Http
    {
        private static async Task<T> ExecuteWithRetry<T>(Func<CancellationToken, Task<T>> action, CancellationToken token)
        {
            int retries = 3;
            int delayMs = 1000;
            for (int i = 0; i < retries; i++)
            {
                try
                {
                    return await action(token);
                }
                catch (Exception ex) when (ex is HttpRequestException || (ex is TaskCanceledException && !token.IsCancellationRequested))
                {
                    App.Logger.WriteLine("Http::ExecuteWithRetry", $"Transient error encountered on attempt {i + 1}: {ex.Message}");
                    if (i == retries - 1)
                        throw;

                    await Task.Delay(delayMs, token);
                    delayMs *= 2; // Exponential backoff
                }
            }
            throw new InvalidOperationException("Unreachable code");
        }

        public static async Task<T> GetJson<T>(string url)
            => await GetJson<T>(url, CancellationToken.None);

        public static async Task<T> GetJson<T>(string url, CancellationToken token)
        {
            return await ExecuteWithRetry(async (t) =>
            {
                var response = await App.HttpClient.GetAsync(url, t);
                response.EnsureSuccessStatusCode();

                string json = await response.Content.ReadAsStringAsync(t);
                return JsonSerializer.Deserialize<T>(json)!;
            }, token);
        }

        public static async Task<T?> PostJson<T>(string url, object body, CancellationToken token = default)
        {
            return await ExecuteWithRetry(async (t) =>
            {
                string jsonBody = JsonSerializer.Serialize(body);
                using var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                using var response = await App.HttpClient.PostAsync(url, content, t);
                response.EnsureSuccessStatusCode();

                string json = await response.Content.ReadAsStringAsync(t);
                return JsonSerializer.Deserialize<T>(json);
            }, token);
        }

        public static async Task<string> GetString(string url, CancellationToken token = default)
        {
            return await ExecuteWithRetry(async (t) =>
            {
                var response = await App.HttpClient.GetAsync(url, t);
                response.EnsureSuccessStatusCode();

                return await response.Content.ReadAsStringAsync(t);
            }, token);
        }
    }
}
