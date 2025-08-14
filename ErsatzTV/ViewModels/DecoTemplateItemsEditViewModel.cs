namespace ErsatzTV.ViewModels;

public class DecoTemplateItemsEditViewModel
{
    public int GroupId { get; set; }
    public string GroupName { get; set; }
    public string Name { get; set; }
    public List<DecoTemplateItemEditViewModel> Items { get; } = [];
}
