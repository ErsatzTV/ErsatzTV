using ErsatzTV.Application.Scheduling;

namespace ErsatzTV.ViewModels;

public class PlayoutTemplateEditViewModel
{
    public int Id { get; set; }
    public int Index { get; set; }
    public TemplateViewModel Template { get; set; }
    public List<DayOfWeek> DaysOfWeek { get; set; }
    public List<int> DaysOfMonth { get; set; }
    public List<int> MonthsOfYear { get; set; }
}
