using ErsatzTV.Application.Scheduling;
using MudBlazor;
using S = System.Collections.Generic;

namespace ErsatzTV.ViewModels;

public class TemplateTreeItemViewModel
{
    public TemplateTreeItemViewModel(TemplateGroupViewModel templateGroup)
    {
        Text = templateGroup.Name;
        TreeItems = [];
        CanExpand = templateGroup.TemplateCount > 0;
        TemplateGroupId = templateGroup.Id;
        Icon = Icons.Material.Filled.Folder;
    }

    public TemplateTreeItemViewModel(TemplateViewModel template)
    {
        Text = template.Name;
        TreeItems = [];
        CanExpand = false;
        TemplateId = template.Id;
    }
    
    public string Text { get; }
    
    public string Icon { get; }
    
    public bool CanExpand { get; }
    
    public int? TemplateId { get; }
    
    public int? TemplateGroupId { get; }
    
    public S.HashSet<TemplateTreeItemViewModel> TreeItems { get; }
}
