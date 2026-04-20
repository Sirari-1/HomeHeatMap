using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

public static class HttpHelper
{
    /// <summary>
    /// Makes an HTTP GET request and deserializes the JSON response.
    /// Logs all request/response details for debugging.
    /// </summary>
    public static async Task<T?> GetJsonWithLoggingAsync<T>(
        HttpClient http, 
        string url, 
        ILogger? logger = null)
    {
        logger?.LogInformation("📡 HTTP GET: {Url}", url);

        using var resp = await http.GetAsync(url);
        var body = await resp.Content.ReadAsStringAsync();

        // Log response details
        logger?.LogInformation(
            "📍 Response: Status={StatusCode}, ContentLength={Length}",
            resp.StatusCode,
            body.Length);

        if (!resp.IsSuccessStatusCode)
        {
            logger?.LogWarning(
                "⚠️ API Error: {Status}\nResponse Body:\n{Body}",
                resp.StatusCode,
                body);
            return default;
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            logger?.LogWarning("⚠️ Empty response body");
            return default;
        }

        // Log raw JSON for inspection
        logger?.LogInformation("📦 Raw Response Body:\n{Body}", body);

        try
        {
            var result = JsonSerializer.Deserialize<T>(body);
            logger?.LogInformation("✅ Successfully deserialized to {Type}", typeof(T).Name);
            return result;
        }
        catch (JsonException ex)
        {
            logger?.LogError(
                ex,
                "❌ JSON Parse Error for type {Type}: {Message}",
                typeof(T).Name,
                ex.Message);
            return default;
        }
    }

    /// <summary>
    /// Logs raw JSON response without deserialization (useful for inspection).
    /// </summary>
    public static async Task<string?> GetRawJsonAsync(HttpClient http, string url, ILogger? logger = null)
    {
        logger?.LogInformation("📡 HTTP GET (raw): {Url}", url);

        using var resp = await http.GetAsync(url);
        var body = await resp.Content.ReadAsStringAsync();

        logger?.LogInformation(
            "📍 Response: Status={StatusCode}, ContentLength={Length}",
            resp.StatusCode,
            body.Length);

        if (!resp.IsSuccessStatusCode)
        {
            logger?.LogWarning("⚠️ API Error {Status}:\n{Body}", resp.StatusCode, body);
            return null;
        }

        logger?.LogInformation("📦 Full Response:\n{Body}", body);
        return body;
    }
}