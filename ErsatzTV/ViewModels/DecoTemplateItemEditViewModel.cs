using Heron.MudCalendar;

namespace ErsatzTV.ViewModels;

public class DecoTemplateItemEditViewModel : CalendarItem
{
    private string _blockName;
    public int DecoId { get; set; }

    public string DecoName
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
