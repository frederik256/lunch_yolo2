namespace LunchYolo2.Services;

public record WeatherData(double Temp, string Condition);

public interface IWeatherService
{
    Task<WeatherData> GetCurrentAsync(double lat, double lon, string timezone);
}
