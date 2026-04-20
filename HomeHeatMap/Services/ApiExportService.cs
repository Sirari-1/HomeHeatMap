using System.Globalization;
using System.Text;
using System.Text.Json;

namespace HomeHeatMap.Services
{
    /// <summary>
    /// Service to export API responses to CSV and JSON files for inspection.
    /// </summary>
    public interface IApiExportService
    {
        Task ExportWeatherApiResponseAsync(double latitude, double longitude, string rawJson);
        Task ExportCrimeApiResponseAsync(string city, string state, string rawJson);
        string GetExportDirectory();
    }

    public class ApiExportService : IApiExportService
    {
        private readonly ILogger<ApiExportService> _logger;
        private readonly string _exportDir;

        public ApiExportService(ILogger<ApiExportService> logger)
        {
            _logger = logger;
            // Export to wwwroot/exports or a temp folder
            _exportDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", "exports");
            Directory.CreateDirectory(_exportDir);
        }

        public string GetExportDirectory() => _exportDir;

        public async Task ExportWeatherApiResponseAsync(double latitude, double longitude, string rawJson)
        {
            try
            {
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                var filename = $"weather_{latitude}_{longitude}_{timestamp}";

                // Export as JSON (prettified)
                var jsonPath = Path.Combine(_exportDir, $"{filename}.json");
                using (var doc = JsonDocument.Parse(rawJson))
                {
                    var prettyJson = JsonSerializer.Serialize(
                        doc.RootElement,
                        new JsonSerializerOptions { WriteIndented = true });
                    await File.WriteAllTextAsync(jsonPath, prettyJson);
                }

                // Export as CSV (flattened)
                await ExportJsonToCsvAsync(rawJson, Path.Combine(_exportDir, $"{filename}.csv"));

                _logger.LogInformation("✅ Weather data exported:\n  📄 {JsonPath}\n  📊 {CsvPath}",
                    Path.GetFileName(jsonPath), Path.GetFileName(Path.Combine(_exportDir, $"{filename}.csv")));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error exporting weather data");
            }
        }

        public async Task ExportCrimeApiResponseAsync(string city, string state, string rawJson)
        {
            try
            {
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                var filename = $"crime_{city}_{state}_{timestamp}";

                // Export as JSON (prettified)
                var jsonPath = Path.Combine(_exportDir, $"{filename}.json");
                using (var doc = JsonDocument.Parse(rawJson))
                {
                    var prettyJson = JsonSerializer.Serialize(
                        doc.RootElement,
                        new JsonSerializerOptions { WriteIndented = true });
                    await File.WriteAllTextAsync(jsonPath, prettyJson);
                }

                // Export as CSV (flattened)
                await ExportJsonToCsvAsync(rawJson, Path.Combine(_exportDir, $"{filename}.csv"));

                _logger.LogInformation("✅ Crime data exported:\n  📄 {JsonPath}\n  📊 {CsvPath}",
                    Path.GetFileName(jsonPath), Path.GetFileName(Path.Combine(_exportDir, $"{filename}.csv")));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error exporting crime data");
            }
        }

        /// <summary>
        /// Flattens a JSON object and exports it to CSV for easy inspection in Excel.
        /// </summary>
        private async Task ExportJsonToCsvAsync(string rawJson, string csvPath)
        {
            try
            {
                using var doc = JsonDocument.Parse(rawJson);
                var root = doc.RootElement;

                // Flatten the JSON
                var flatData = FlattenJsonElement(root);

                // Write to CSV
                var csv = new StringBuilder();
                csv.AppendLine("Field,Value,Type");

                foreach (var row in flatData)
                {
                    var field = EscapeCsvField(row.Key);
                    var value = EscapeCsvField(row.Value);
                    var type = EscapeCsvField(row.Type);
                    csv.AppendLine($"{field},{value},{type}");
                }

                await File.WriteAllTextAsync(csvPath, csv.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error converting JSON to CSV");
            }
        }

        /// <summary>
        /// Escapes CSV field values to handle commas, quotes, and newlines.
        /// </summary>
        private string EscapeCsvField(string field)
        {
            if (string.IsNullOrEmpty(field))
                return "\"\"";

            if (field.Contains(",") || field.Contains("\"") || field.Contains("\n"))
            {
                return $"\"{field.Replace("\"", "\"\"")}\"";
            }

            return field;
        }

        /// <summary>
        /// Recursively flattens a JSON element into key-value pairs.
        /// Example: { "main": { "temp": 25 } } becomes "main.temp" = "25"
        /// </summary>
        private List<FlatJsonRow> FlattenJsonElement(
            JsonElement element,
            string prefix = "")
        {
            var rows = new List<FlatJsonRow>();

            if (element.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in element.EnumerateObject())
                {
                    var key = string.IsNullOrEmpty(prefix) ? prop.Name : $"{prefix}.{prop.Name}";
                    rows.AddRange(FlattenJsonElement(prop.Value, key));
                }
            }
            else if (element.ValueKind == JsonValueKind.Array)
            {
                int index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    var key = $"{prefix}[{index}]";
                    rows.AddRange(FlattenJsonElement(item, key));
                    index++;
                }
            }
            else
            {
                rows.Add(new FlatJsonRow
                {
                    Key = prefix,
                    Value = element.GetRawText(),
                    Type = element.ValueKind.ToString()
                });
            }

            return rows;
        }
    }

    /// <summary>
    /// Represents a flattened JSON key-value pair for CSV export.
    /// </summary>
    public class FlatJsonRow
    {
        public string Key { get; set; } = "";
        public string Value { get; set; } = "";
        public string Type { get; set; } = "";
    }
}