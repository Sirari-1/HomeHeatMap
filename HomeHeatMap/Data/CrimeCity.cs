namespace HomeHeatMap.Data
{
    public class CrimeCity
    {
        public int Id { get; set; }
        public string Slug { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public int Population { get; set; }
        public int ViolentCrime { get; set; }
        public double ViolentRate { get; set; }
        public int Murder { get; set; }
        public double MurderRate { get; set; }
        public int PropertyCrime { get; set; }
        public double PropertyRate { get; set; }
        public int Year { get; set; }
        public double ViolentChange { get; set; }
        public string Trajectory { get; set; } = string.Empty;
        public double ViolentToPropertyRatio { get; set; }
        public int SafetyPercentile { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }
}