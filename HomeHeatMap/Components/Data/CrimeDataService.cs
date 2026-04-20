using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using HomeHeatMap.Services;

namespace HomeHeatMap.Components.Data
{
    public interface ICrimeDataService
    {
        Task<double> GetCrimeSafetyScoreAsync(string city, string state);
    }

    public class CrimeDataService : ICrimeDataService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<CrimeDataService> _logger;
        private readonly IApiExportService _exportService;
        private const string BaseUrl = "https://crime-data-api.fr.cloud.gov/api";

        public CrimeDataService(
            IHttpClientFactory httpClientFactory,
            ILogger<CrimeDataService> logger,
            IApiExportService exportService)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _exportService = exportService;
        }

        public async Task<double> GetCrimeSafetyScoreAsync(string city, string state)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var url = $"{BaseUrl}/incidents?place={city}%2C{state}&page=0";

                _logger.LogInformation("🔍 Fetching crime data for: {City}, {State}", city, state);

                // Get raw response for inspection
                var rawJson = await HttpHelper.GetRawJsonAsync(client, url, _logger);
                if (string.IsNullOrEmpty(rawJson))
                {
                    _logger.LogWarning("❌ No crime data received for {City}, {State}", city, state);
                    return 50; // Default middle score
                }

                // Export to file for inspection
                await _exportService.ExportCrimeApiResponseAsync(city, state, rawJson);

                using var doc = JsonDocument.Parse(rawJson);
                var root = doc.RootElement;

                _logger.LogInformation("📊 Parsed JSON root element: {RootType}", root.ValueKind);

                // Pretty-print the entire response for debugging
                var prettyJson = JsonSerializer.Serialize(
                    doc.RootElement,
                    new JsonSerializerOptions { WriteIndented = true });
                _logger.LogInformation("🎯 Complete Crime Data Response:\n{JsonData}", prettyJson);

                // Extract crime statistics
                if (root.TryGetProperty("data", out var dataArray) && dataArray.GetArrayLength() > 0)
                {
                    var firstResult = dataArray[0];
                    _logger.LogInformation("📍 First result keys: {Keys}", 
                        string.Join(", ", dataArray[0].EnumerateObject().Select(p => p.Name)));

                    if (firstResult.TryGetProperty("violent_crime", out var violentCrime))
                    {
                        var crimeRate = violentCrime.GetDouble();
                        var safetyScore = Math.Max(0, 100 - (crimeRate / 15));
                        _logger.LogInformation("✅ Safety Score: {Score} (from violent crime rate: {Rate})", safetyScore, crimeRate);
                        return safetyScore;
                    }
                }

                _logger.LogWarning("⚠️ No 'data' array found in response");
                return 50;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error fetching crime data for {City}, {State}", city, state);
                return 50;
            }
        }
    }
}