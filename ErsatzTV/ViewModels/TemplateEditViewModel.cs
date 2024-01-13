namespace ErsatzTV.ViewModels;

public class TemplateEditViewModel
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int Index { get; set; }
    public List<DayOfWeek> DaysOfWeek { get; set; }
    public List<int> MonthsOfYear { get; set; }
}
