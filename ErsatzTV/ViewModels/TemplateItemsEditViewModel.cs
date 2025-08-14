namespace ErsatzTV.ViewModels;

public class TemplateItemsEditViewModel
{
    public int GroupId { get; set; }
    public string GroupName { get; set; }
    public string Name { get; set; }
    public List<TemplateItemEditViewModel> Items { get; } = [];
}
