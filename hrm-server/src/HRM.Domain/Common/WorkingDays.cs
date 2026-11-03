namespace HRM.Domain.Common;

public static class WorkingDays
{
    /// <summary>Counts Monday–Friday days in [start, end], both inclusive. Public holidays are not excluded yet.</summary>
    public static int Count(DateOnly start, DateOnly end)
    {
        var count = 0;
        for (var day = start; day <= end; day = day.AddDays(1))
        {
            if (day.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday))
                count++;
        }
        return count;
    }
}
