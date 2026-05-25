using LunchYolo2.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient<WeatherService>();
builder.Services.AddHttpClient<FtseService>();
builder.Services.AddScoped<IWeatherService, WeatherService>();
builder.Services.AddScoped<IFtseService, FtseService>();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/weather", async (IWeatherService svc) => await svc.GetCurrentAsync());
app.MapGet("/api/ftse", async (IFtseService svc) => await svc.GetWeeklyAsync());

app.Run();
