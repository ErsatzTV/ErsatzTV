namespace ErsatzTV.Core.Domain;

public class ProgramScheduleItemGraphicsElement
{
    public int ProgramScheduleItemId { get; set; }
    public ProgramScheduleItem ProgramScheduleItem { get; set; }
    public int GraphicsElementId { get; set; }
    public GraphicsElement GraphicsElement { get; set; }
}
