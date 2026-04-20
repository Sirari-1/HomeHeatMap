using System.Data.Common;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace HomeHeatMap.Data;

public static class CrimeCityCoordinatesBackfill
{
    public static async Task EnsureCoordinateColumnsAsync(AppDbContext db, ILogger logger)
    {
        var columns = await GetCrimeCityColumnsAsync(db);

        if (!columns.Contains("Latitude", StringComparer.OrdinalIgnoreCase))
        {
            await db.Database.ExecuteSqlRawAsync("ALTER TABLE CrimeCities ADD COLUMN Latitude REAL NULL;");
            logger.LogInformation("?? Added CrimeCities.Latitude column");
        }

        if (!columns.Contains("Longitude", StringComparer.OrdinalIgnoreCase))
        {
            await db.Database.ExecuteSqlRawAsync("ALTER TABLE CrimeCities ADD COLUMN Longitude REAL NULL;");
            logger.LogInformation("?? Added CrimeCities.Longitude column");
        }
    }

    public static async Task PopulateMissingCoordinatesAsync(AppDbContext db, IHttpClientFactory httpClientFactory, ILogger logger)
    {
        var missing = await db.CrimeCities
            .Where(c => c.State == "Florida" && (!c.Latitude.HasValue || !c.Longitude.HasValue))
            .OrderBy(c => c.City)
            .ToListAsync();

        if (missing.Count == 0)
        {
            logger.LogInformation("? All Florida cities already have stored coordinates");
            return;
        }

        logger.LogInformation("?? Backfilling coordinates for {Count} cities...", missing.Count);

        var httpClient = httpClientFactory.CreateClient();
        var updated = 0;
        var processed = 0;

        foreach (var city in missing)
        {
            processed++;

            try
            {
                var coords = await GeocodeCityAsync(httpClient, city.City, city.State);
                if (coords is not null)
                {
                    city.Latitude = coords.Value.lat;
                    city.Longitude = coords.Value.lon;
                    updated++;
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "?? Failed to geocode {City}, {State}", city.City, city.State);
            }

            if (processed % 25 == 0)
            {
                await db.SaveChangesAsync();
                logger.LogInformation("?? Coordinate backfill progress: {Processed}/{Total}", processed, missing.Count);
            }

            await Task.Delay(40);
        }

        await db.SaveChangesAsync();
        logger.LogInformation("? Coordinate backfill done. Updated {Updated} of {Total} cities", updated, missing.Count);
    }

    private static async Task<(double lat, double lon)?> GeocodeCityAsync(HttpClient httpClient, string city, string state)
    {
        var query = Uri.EscapeDataString(city);
        var response = await httpClient.GetAsync($"https://geocoding-api.open-meteo.com/v1/search?name={query}&count=10&language=en&format=json");

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);

        if (!doc.RootElement.TryGetProperty("results", out var results) || results.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        JsonElement? firstUs = null;

        foreach (var item in results.EnumerateArray())
        {
            var countryCode = item.TryGetProperty("country_code", out var cc) ? cc.GetString() : null;
            var admin1 = item.TryGetProperty("admin1", out var a1) ? a1.GetString() : null;

            if (!string.Equals(countryCode, "US", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            firstUs ??= item;

            if (string.Equals(admin1, state, StringComparison.OrdinalIgnoreCase))
            {
                var lat = item.TryGetProperty("latitude", out var latProp) ? latProp.GetDouble() : double.NaN;
                var lon = item.TryGetProperty("longitude", out var lonProp) ? lonProp.GetDouble() : double.NaN;

                if (double.IsFinite(lat) && double.IsFinite(lon))
                {
                    return (lat, lon);
                }
            }
        }

        if (firstUs.HasValue)
        {
            var fallback = firstUs.Value;
            var lat = fallback.TryGetProperty("latitude", out var latProp) ? latProp.GetDouble() : double.NaN;
            var lon = fallback.TryGetProperty("longitude", out var lonProp) ? lonProp.GetDouble() : double.NaN;

            if (double.IsFinite(lat) && double.IsFinite(lon))
            {
                return (lat, lon);
            }
        }

        return null;
    }

    private static async Task<HashSet<string>> GetCrimeCityColumnsAsync(AppDbContext db)
    {
        var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        await using DbConnection connection = db.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA table_info('CrimeCities');";

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            if (!reader.IsDBNull(1))
            {
                columns.Add(reader.GetString(1));
            }
        }

        return columns;
    }
}
