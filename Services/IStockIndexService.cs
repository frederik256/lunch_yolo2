namespace LunchYolo2.Services;

public record StockIndexData(DateOnly[] Dates, double[] Closes, string DateRange);

public interface IStockIndexService
{
    Task<StockIndexData> GetWeeklyAsync(string ticker);
}
