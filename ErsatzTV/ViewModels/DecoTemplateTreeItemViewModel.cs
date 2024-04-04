using ErsatzTV.Application.Scheduling;
using MudBlazor;
using S = System.Collections.Generic;

namespace ErsatzTV.ViewModels;

public class DecoTemplateTreeItemViewModel
{
    public DecoTemplateTreeItemViewModel(DecoTemplateGroupViewModel decoTemplateGroup)
    {
        Text = decoTemplateGroup.Name;
        TreeItems = [];
        CanExpand = decoTemplateGroup.DecoTemplateCount > 0;
        DecoTemplateGroupId = decoTemplateGroup.Id;
        Icon = Icons.Material.Filled.Folder;
    }

    public DecoTemplateTreeItemViewModel(DecoTemplateViewModel decoTemplate)
    {
        Text = decoTemplate.Name;
        TreeItems = [];
        CanExpand = false;
        DecoTemplateId = decoTemplate.Id;
    }

    public string Text { get; }

    public string Icon { get; }

    public bool CanExpand { get; }

    public int? DecoTemplateId { get; }

    public int? DecoTemplateGroupId { get; }

    public S.HashSet<DecoTemplateTreeItemViewModel> TreeItems { get; }
}
