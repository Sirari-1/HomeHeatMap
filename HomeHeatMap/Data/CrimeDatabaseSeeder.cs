using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace HomeHeatMap.Data
{
    public static class CrimeDatabaseSeeder
    {
        public static async Task SeedAsync(AppDbContext db, string jsonFilePath, ILogger logger)
        {
            if (!File.Exists(jsonFilePath))
            {
                logger.LogWarning("⚠️ city-index.json not found at: {Path}", jsonFilePath);
                return;
            }

            logger.LogInformation("📥 Seeding crime database from {Path}...", jsonFilePath);

            var json = await File.ReadAllTextAsync(jsonFilePath);
            var cities = JsonSerializer.Deserialize<List<CrimeCityJson>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (cities == null || cities.Count == 0)
            {
                logger.LogWarning("⚠️ No cities found in JSON");
                return;
            }

            var entities = cities
                .Where(c => c.State != null && c.State.Equals("Florida", StringComparison.OrdinalIgnoreCase))
                .Select(c => new CrimeCity
                {
                    Slug                   = c.Slug ?? string.Empty,
                    City                   = c.City ?? string.Empty,
                    State                  = c.State ?? string.Empty,
                    Population             = c.Population,
                    ViolentCrime           = c.ViolentCrime,
                    ViolentRate            = c.ViolentRate,
                    Murder                 = c.Murder,
                    MurderRate             = c.MurderRate,
                    PropertyCrime          = c.PropertyCrime,
                    PropertyRate           = c.PropertyRate,
                    Year                   = c.Year,
                    ViolentChange          = c.ViolentChange,
                    Trajectory             = c.Trajectory ?? string.Empty,
                    ViolentToPropertyRatio = c.ViolentToPropertyRatio,
                    SafetyPercentile       = c.SafetyPercentile,
                    Latitude               = c.Latitude ?? c.Lat,
                    Longitude              = c.Longitude ?? c.Lon ?? c.Lng
                }).ToList();

            if (entities.Count == 0)
            {
                logger.LogWarning("⚠️ JSON loaded but no Florida cities were found. Existing database data was kept.");
                return;
            }

            // Replace data only after JSON is confirmed valid.
            var existingCount = await db.CrimeCities.CountAsync();
            if (existingCount > 0)
            {
                await db.CrimeCities.ExecuteDeleteAsync();
                logger.LogInformation("♻️ Cleared existing crime data ({Count} cities)", existingCount);
            }

            await db.CrimeCities.AddRangeAsync(entities);
            await db.SaveChangesAsync();

            logger.LogInformation("✅ Seeded {Count} cities into crime database", entities.Count);
        }

        // JSON deserialization model matching city-index.json fields
        private class CrimeCityJson
        {
            public string? Slug { get; set; }
            public string? City { get; set; }
            public string? State { get; set; }
            public int Population { get; set; }
            public int ViolentCrime { get; set; }
            public double ViolentRate { get; set; }
            public int Murder { get; set; }
            public double MurderRate { get; set; }
            public int PropertyCrime { get; set; }
            public double PropertyRate { get; set; }
            public int Year { get; set; }
            public double ViolentChange { get; set; }
            public string? Trajectory { get; set; }
            public double ViolentToPropertyRatio { get; set; }
            public int SafetyPercentile { get; set; }
            public double? Latitude { get; set; }
            public double? Longitude { get; set; }

            [JsonPropertyName("lat")]
            public double? Lat { get; set; }

            [JsonPropertyName("lon")]
            public double? Lon { get; set; }

            [JsonPropertyName("lng")]
            public double? Lng { get; set; }
        }
    }
}           