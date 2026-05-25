namespace LunchYolo2.Services;

public record FtseData(DateOnly[] Dates, double[] Closes, string DateRange);

public interface IFtseService
{
    Task<FtseData> GetWeeklyAsync();
}
