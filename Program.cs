using LunchYolo2.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient<IWeatherService, WeatherService>();
builder.Services.AddHttpClient<IStockIndexService, StockIndexService>();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/offices", async (IWeatherService weatherSvc, IStockIndexService indexSvc, IConfiguration config) =>
{
    var offices = config.GetSection("Offices").Get<OfficeConfig[]>() ?? [];
    var results = await Task.WhenAll(offices.Select(async o =>
    {
        try
        {
            var weatherTask = weatherSvc.GetCurrentAsync(o.Lat, o.Lon, o.Timezone);
            var indexTask = indexSvc.GetWeeklyAsync(o.IndexTicker);
            await Task.WhenAll(weatherTask, indexTask);
            var idx = indexTask.Result;
            return new
            {
                name = o.Name,
                weather = (object?)weatherTask.Result,
                index = (object?)new { name = o.IndexName, idx.Dates, idx.Closes, idx.DateRange },
                error = (string?)null
            };
        }
        catch (Exception ex)
        {
            return new
            {
                name = o.Name,
                weather = (object?)null,
                index = (object?)null,
                error = (string?)ex.Message
            };
        }
    }));
    return Results.Json(results);
});

app.Run();

record OfficeConfig(string Name, double Lat, double Lon, string Timezone, string IndexTicker, string IndexName);
