using LunchYolo2.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient<WeatherService>();
builder.Services.AddHttpClient<StockIndexService>();
builder.Services.AddScoped<IWeatherService, WeatherService>();
builder.Services.AddScoped<IStockIndexService, StockIndexService>();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/offices", async (IWeatherService weatherSvc, IStockIndexService indexSvc, IConfiguration config) =>
{
    var offices = config.GetSection("Offices").Get<OfficeConfig[]>() ?? [];
    var results = await Task.WhenAll(offices.Select(async o =>
    {
        var weatherTask = weatherSvc.GetCurrentAsync(o.Lat, o.Lon, o.Timezone);
        var indexTask = indexSvc.GetWeeklyAsync(o.IndexTicker);
        await Task.WhenAll(weatherTask, indexTask);
        var idx = indexTask.Result;
        return new
        {
            name = o.Name,
            weather = weatherTask.Result,
            index = new { name = o.IndexName, idx.Dates, idx.Closes, idx.DateRange }
        };
    }));
    return Results.Json(results);
});

app.Run();

record OfficeConfig(string Name, double Lat, double Lon, string Timezone, string IndexTicker, string IndexName);
