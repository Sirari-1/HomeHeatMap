using HomeHeatMap.Components;
using HomeHeatMap.Components.Data;
using HomeHeatMap.Data;
using HomeHeatMap.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.SetMinimumLevel(LogLevel.Information);

// Register SQLite database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=crimedata.db"));

// Register services
builder.Services.AddHttpClient();
builder.Services.AddScoped<ICrimeDataService, CrimeDataService>();
builder.Services.AddScoped<IDebugService, DebugService>();
builder.Services.AddScoped<IApiExportService, ApiExportService>();
builder.Services.AddScoped<ICrimeDataRepository, CrimeDataRepository>();

var app = builder.Build();

// Seed database from city-index.json on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var httpClientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    await db.Database.EnsureCreatedAsync();
    await CrimeCityCoordinatesBackfill.EnsureCoordinateColumnsAsync(db, logger);

    // Try common locations for city-index.json
    var candidateJsonPaths = new[]
    {
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Downloads", "city-index.json"),
        Path.Combine(app.Environment.ContentRootPath, "city-index.json"),
        Path.Combine(app.Environment.ContentRootPath, "Data", "city-index.json")
    };

    var jsonPath = candidateJsonPaths.FirstOrDefault(File.Exists) ?? candidateJsonPaths[0];
    logger.LogInformation("📄 city-index.json path selected: {Path}", jsonPath);

    var removed = await db.CrimeCities
        .Where(c => c.State != "Florida")
        .ExecuteDeleteAsync();

    if (removed > 0)
        logger.LogInformation("🗑️ Removed {Count} non-Florida cities from database", removed);

    await CrimeDatabaseSeeder.SeedAsync(db, jsonPath, logger);
    await CrimeCityCoordinatesBackfill.PopulateMissingCoordinatesAsync(db, httpClientFactory, logger);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add these BEFORE app.Run()
app.MapGet("/api/cities", async (ICrimeDataRepository repo) =>
{
    var cities = await repo.GetAllCitiesAsync();
    return Results.Ok(cities);
});

app.MapGet("/api/cities/{state}/{city}", async (string state, string city, ICrimeDataRepository repo) =>
{
    var result = await repo.GetCityAsync(city, state);
    return result is not null ? Results.Ok(result) : Results.NotFound();
});

app.MapGet("/api/florida", async (ICrimeDataRepository repo) =>
{
    var cities = await repo.GetAllCitiesAsync();
    return Results.Ok(cities.Select(c => new
    {
        c.City,
        c.State,
        c.Population,
        c.ViolentCrime,
        c.ViolentRate,
        c.Murder,
        c.MurderRate,
        c.PropertyCrime,
        c.PropertyRate,
        c.Year,
        c.ViolentChange,
        c.Trajectory,
        c.ViolentToPropertyRatio,
        c.SafetyPercentile,
        c.Latitude,
        c.Longitude
    }));
});

app.Run();
