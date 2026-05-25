using System.Text.Json;

namespace LunchYolo2.Services;

public class FtseService(HttpClient http) : IFtseService
{
    private const string Url =
        "https://query1.finance.yahoo.com/v8/finance/chart/%5EFTSE?interval=1d&range=5d";

    public async Task<FtseData> GetWeeklyAsync()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, Url);
        request.Headers.Add("User-Agent", "Mozilla/5.0");
        var response = await http.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(json);
        var result = doc.RootElement
            .GetProperty("chart").GetProperty("result")[0];

        var timestamps = result.GetProperty("timestamp").EnumerateArray()
            .Select(t => DateTimeOffset.FromUnixTimeSeconds(t.GetInt64()).UtcDateTime)
            .ToArray();

        var closes = result.GetProperty("indicators")
            .GetProperty("quote")[0]
            .GetProperty("close").EnumerateArray()
            .Select(c => Math.Round(c.GetDouble(), 2))
            .ToArray();

        var dates = timestamps.Select(DateOnly.FromDateTime).ToArray();

        var dateRange = dates.Length > 0
            ? $"{dates[0]:d MMM} – {dates[^1]:d MMM}"
            : string.Empty;

        return new FtseData(dates, closes, dateRange);
    }
}
