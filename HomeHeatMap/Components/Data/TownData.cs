using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HomeHeatMap.Components.Data
{
    public class Town
    {
        public string Name { get; set; } = string.Empty;
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public Dictionary<string, double> Metrics { get; set; } = new();

        public double GetScore(string metric)
        {
            if (Metrics != null && Metrics.TryGetValue(metric, out var v))
                return v;
            return 0;
        }
    }

    public static class TownRepository
    {
        public static readonly string[] MetricKeys = new[]
        {
            "Safety/Crime",
            "Cost of Living",
            "Sunny Days",
            "Rainy Days",
            "Avg Yearly Temp",
            "Hospitals/Medical",
            "Community/Social",
            "Pests/Hazardous Animals",
            "Weather Hazards",
            "Rural vs Metro",
            "Commuting to Metro",
            "Avg Home Price",
            "Recreation/Outdoors"
        };

        private static readonly List<(string Name, double Lat, double Lon, string City, string State)> Towns = new()
        {
            ("Pahokee",     26.8196, -80.6859, "Pahokee",     "FL"),
            ("Belle Glade", 26.6886, -80.6553, "Belle Glade", "FL"),
            ("Clewiston",   26.7669, -80.9362, "Clewiston",   "FL"),
            ("Okeechobee",  27.2414, -80.8278, "Okeechobee",  "FL"),
            ("South Bay",   26.6608, -80.7238, "South Bay",   "FL"),
            ("Port Mayaca", 27.0046, -80.6984, "Port Mayaca", "FL"),
            ("Indiantown",  26.8599, -80.5506, "Indiantown",  "FL"),
            ("Hastings",    27.5938, -81.3994, "Hastings",    "FL"),
            ("Stuart",      27.1689, -80.2410, "Stuart",      "FL")
        };

        public static async Task<List<Town>> GetTownsAsync(ICrimeDataService crimeDataService)
        {
            var townList = new List<Town>();

            foreach (var (name, lat, lon, city, state) in Towns)
            {
                var town = new Town
                {
                    Name = name,
                    Latitude = lat,
                    Longitude = lon,
                    Metrics = new Dictionary<string, double>()
                };

                try
                {
                    var safetyScore = await crimeDataService.GetCrimeSafetyScoreAsync(city, state);
                    town.Metrics["Safety/Crime"] = safetyScore;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading data for {name}: {ex.Message}");
                }

                town.Metrics.TryAdd("Avg Yearly Temp",         70);
                town.Metrics.TryAdd("Sunny Days",              75);
                town.Metrics.TryAdd("Rainy Days",              55);
                town.Metrics.TryAdd("Cost of Living",          70);
                town.Metrics.TryAdd("Hospitals/Medical",       65);
                town.Metrics.TryAdd("Community/Social",        60);
                town.Metrics.TryAdd("Pests/Hazardous Animals", 50);
                town.Metrics.TryAdd("Weather Hazards",         55);
                town.Metrics.TryAdd("Rural vs Metro",          40);
                town.Metrics.TryAdd("Commuting to Metro",      50);
                town.Metrics.TryAdd("Avg Home Price",          60);
                town.Metrics.TryAdd("Recreation/Outdoors",     75);

                townList.Add(town);
            }

            return townList;
        }
    }
}
