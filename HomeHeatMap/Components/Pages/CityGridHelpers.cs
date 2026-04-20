namespace HomeHeatMap.Components.Pages;

public static class CityGridHelpers
{
    public static string GetSafetyGrade(double violentRate) => violentRate switch
    {
        < 100  => "A+",
        < 200  => "A",
        < 300  => "B",
        < 400  => "C",
        < 600  => "D",
        < 1000 => "F",
        _      => "F-"
    };

    public static string GetGradeColor(string grade) => grade switch
    {
        "A+" => "rgb(0,140,30)",
        "A"  => "rgb(40,160,40)",
        "B"  => "rgb(100,160,0)",
        "C"  => "rgb(180,140,0)",
        "D"  => "rgb(200,80,0)",
        "F"  => "rgb(180,0,30)",
        "F-" => "rgb(120,0,20)",
        _    => "inherit"
    };

    public static string GetPercentileColor(int percentile) => percentile switch
    {
        <= 10 => "rgb(0,140,30)",
        <= 25 => "rgb(40,160,40)",
        <= 50 => "rgb(100,160,0)",
        <= 65 => "rgb(180,140,0)",
        <= 80 => "rgb(200,80,0)",
        <= 90 => "rgb(180,0,30)",
        _     => "rgb(120,0,20)"
    };
}