using System.Text.Json;

namespace LunchYolo2.Services;

public class WeatherService(HttpClient http) : IWeatherService
{
    public async Task<WeatherData> GetCurrentAsync(double lat, double lon, string timezone)
    {
        var url = $"https://api.open-meteo.com/v1/forecast?latitude={lat}&longitude={lon}&current=temperature_2m,weathercode&timezone={Uri.EscapeDataString(timezone)}";
        var json = await http.GetStringAsync(url);
        using var doc = JsonDocument.Parse(json);
        var current = doc.RootElement.GetProperty("current");
        var temp = current.GetProperty("temperature_2m").GetDouble();
        var code = current.GetProperty("weathercode").GetInt32();
        return new WeatherData(temp, MapCode(code));
    }

    private static string MapCode(int code) => code switch
    {
        0 => "Clear",
        1 or 2 => "Partly Cloudy",
        3 => "Cloudy",
        45 or 48 => "Foggy",
        51 or 53 or 55 or 61 or 63 or 65 or 80 or 81 or 82 => "Rain",
        71 or 73 or 75 or 85 or 86 => "Snow",
        95 or 96 or 99 => "Thunderstorm",
        _ => "Cloudy"
    };
}
