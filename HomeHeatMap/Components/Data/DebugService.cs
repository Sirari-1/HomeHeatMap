using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace HomeHeatMap.Components.Data
{
    public interface IDebugService
    {
        Task LogCrimeApiResponseAsync(string city, string state);
    }

    public class DebugService : IDebugService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public DebugService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task LogCrimeApiResponseAsync(string city, string state)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var url = $"https://crime-data-api.fr.cloud.gov/api/incidents?place={city}%2C{state}&page=0";

                Console.WriteLine($"\n=== CRIME API REQUEST ===");
                Console.WriteLine($"URL: {url}");

                var response = await client.GetAsync(url);
                var json = await response.Content.ReadAsStringAsync();

                Console.WriteLine($"RESPONSE STATUS: {response.StatusCode}");
                Console.WriteLine($"RAW JSON RESPONSE:\n{json}");

                using var doc = JsonDocument.Parse(json);
                var prettyJson = JsonSerializer.Serialize(doc.RootElement,
                    new JsonSerializerOptions { WriteIndented = true });
                Console.WriteLine($"FORMATTED JSON:\n{prettyJson}\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
            }
        }
    }
}