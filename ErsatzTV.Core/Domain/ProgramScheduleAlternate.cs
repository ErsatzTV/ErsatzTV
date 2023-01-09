namespace ErsatzTV.Core.Domain;

public class ProgramScheduleAlternate
{
    public static List<DayOfWeek> AllDaysOfWeek() => new()
    {
        DayOfWeek.Monday,
        DayOfWeek.Tuesday,
        DayOfWeek.Wednesday,
        DayOfWeek.Thursday,
        DayOfWeek.Friday,
        DayOfWeek.Saturday,
        DayOfWeek.Sunday
    };

    public static List<int> AllDaysOfMonth() => Enumerable.Range(1, 31).ToList();
    public static List<int> AllMonthsOfYear() => Enumerable.Range(1, 12).ToList();
    
    public int Id { get; set; }
    public int PlayoutId { get; set; }
    public Playout Playout { get; set; }
    public int ProgramScheduleId { get; set; }
    public ProgramSchedule ProgramSchedule { get; set; }
    public int Index { get; set; }
    public ICollection<DayOfWeek> DaysOfWeek { get; set; }
    public ICollection<int> DaysOfMonth { get; set; }
    public ICollection<int> MonthsOfYear { get; set; }
}
