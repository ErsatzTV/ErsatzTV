namespace ErsatzTV.ViewModels;

public class DecoTemplateItemsEditViewModel
{
    public string GroupName { get; set; }
    public string Name { get; set; }
    public List<DecoTemplateItemEditViewModel> Items { get; } = [];
}
