namespace ErsatzTV.Core.Domain;

public class ProgramScheduleItemMultiple : ProgramScheduleItem
{
    public MultipleMode MultipleMode { get; set; }

    public int Count { get; set; }
}
