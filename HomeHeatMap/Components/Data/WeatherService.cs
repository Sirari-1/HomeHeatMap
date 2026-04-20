using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using HomeHeatMap.Services;

namespace HomeHeatMap.Components.Data
{
    public class WeatherData
    {
        public double? AvgTemp { get; set; }
        public int? SunnyDays { get; set; }
        public int? RainyDays { get; set; }
    }

    public interface IWeatherService
    {
        Task<WeatherData> GetWeatherDataAsync(double latitude, double longitude);
    }

    public class WeatherService : IWeatherService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _apiKey;
        private readonly ILogger<WeatherService> _logger;
        private readonly IApiExportService _exportService;
        private const string BaseUrl = "https://api.openweathermap.org/data/2.5";

        public WeatherService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<WeatherService> logger,
            IApiExportService exportService)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _exportService = exportService;
            _apiKey = configuration["OpenWeatherMap:ApiKey"] ?? "";
        }

        public async Task<WeatherData> GetWeatherDataAsync(double latitude, double longitude)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var url = $"{BaseUrl}/weather?lat={latitude}&lon={longitude}&appid={_apiKey}&units=metric";

                _logger.LogInformation("🌡️ Fetching weather for: Lat={Lat}, Lon={Lon}", latitude, longitude);

                // Get raw JSON for inspection
                var rawJson = await HttpHelper.GetRawJsonAsync(client, url, _logger);
                if (string.IsNullOrEmpty(rawJson))
                {
                    _logger.LogWarning("❌ No weather data received");
                    return new WeatherData();
                }

                // Export to file for inspection
                await _exportService.ExportWeatherApiResponseAsync(latitude, longitude, rawJson);

                using var doc = JsonDocument.Parse(rawJson);
                var root = doc.RootElement;

                // Pretty-print entire response
                var prettyJson = JsonSerializer.Serialize(
                    root,
                    new JsonSerializerOptions { WriteIndented = true });
                _logger.LogInformation("🎯 Complete Weather Data Response:\n{JsonData}", prettyJson);

                var temp = root.GetProperty("main").GetProperty("temp").GetDouble();
                _logger.LogInformation("✅ Temperature: {Temp}°C", temp);

                return new WeatherData { AvgTemp = temp };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error fetching weather for Lat={Lat}, Lon={Lon}", latitude, longitude);
                return new WeatherData();
            }
        }
    }
}