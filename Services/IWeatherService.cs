namespace LunchYolo2.Services;

public record WeatherData(double Temp, string Condition);

public interface IWeatherService
{
    Task<WeatherData> GetCurrentAsync();
}
