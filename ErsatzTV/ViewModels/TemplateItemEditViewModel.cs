using Heron.MudCalendar;

namespace ErsatzTV.ViewModels;

public class TemplateItemEditViewModel : CalendarItem
{
    private string _blockName;
    public int BlockId { get; set; }

    public string BlockName
    {
        get => _blockName;
        set
        {
            _blockName = value;
            Text = value;
        }
    }
    
    public DateTime LastStart { get; set; }
    public DateTime? LastEnd { get; set; }
}
