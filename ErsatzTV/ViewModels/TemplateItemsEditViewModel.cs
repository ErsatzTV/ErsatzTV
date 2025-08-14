namespace ErsatzTV.ViewModels;

public class TemplateItemsEditViewModel
{
    public string GroupName { get; set; }
    public string Name { get; set; }
    public List<TemplateItemEditViewModel> Items { get; } = [];
}
