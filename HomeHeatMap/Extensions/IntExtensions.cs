namespace HomeHeatMap.Extensions
{
    public static class IntExtensions
    {
        public static string ToLocaleString(this int value) =>
            value.ToString("N0");
    }
}