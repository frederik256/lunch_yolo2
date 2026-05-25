using System.Text.Json;

namespace LunchYolo2.Services;

public class StockIndexService(HttpClient http) : IStockIndexService
{
    public async Task<StockIndexData> GetWeeklyAsync(string ticker)
    {
        var url = $"https://query1.finance.yahoo.com/v8/finance/chart/{Uri.EscapeDataString(ticker)}?interval=1d&range=5d";
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("User-Agent", "Mozilla/5.0");
        var response = await http.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(json);
        var result = doc.RootElement
            .GetProperty("chart").GetProperty("result")[0];

        var rawTimestamps = result.GetProperty("timestamp").EnumerateArray()
            .Select(t => DateTimeOffset.FromUnixTimeSeconds(t.GetInt64()).UtcDateTime)
            .ToArray();

        var rawCloses = result.GetProperty("indicators")
            .GetProperty("quote")[0]
            .GetProperty("close").EnumerateArray()
            .ToArray();

        var pairs = rawTimestamps.Zip(rawCloses)
            .Where(p => p.Second.ValueKind != JsonValueKind.Null)
            .ToArray();

        var dates = pairs.Select(p => DateOnly.FromDateTime(p.First)).ToArray();
        var closes = pairs.Select(p => Math.Round(p.Second.GetDouble(), 2)).ToArray();

        var dateRange = dates.Length > 0
            ? $"{dates[0]:d MMM} – {dates[^1]:d MMM}"
            : string.Empty;

        return new StockIndexData(dates, closes, dateRange);
    }
}
