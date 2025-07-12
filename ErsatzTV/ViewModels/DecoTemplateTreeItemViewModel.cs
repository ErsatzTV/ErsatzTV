using ErsatzTV.Application.Scheduling;
using ErsatzTV.Application.Tree;
using MudBlazor;

namespace ErsatzTV.ViewModels;

public class DecoTemplateTreeItemViewModel
{
    public DecoTemplateTreeItemViewModel(DecoTemplateGroupViewModel decoTemplateGroup)
    {
        Text = decoTemplateGroup.Name;
        TreeItems = [];
        DecoTemplateGroupId = decoTemplateGroup.Id;
        Icon = Icons.Material.Filled.Folder;
    }

    public DecoTemplateTreeItemViewModel(TreeGroupViewModel decoTemplateGroup)
    {
        Text = decoTemplateGroup.Name;
        TreeItems = decoTemplateGroup.Children.Map(d => new TreeItemData<DecoTemplateTreeItemViewModel>
            { Value = new DecoTemplateTreeItemViewModel(d) }).ToList();
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

    public DecoTemplateTreeItemViewModel(TreeItemViewModel decoTemplate)
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

    public List<TreeItemData<DecoTemplateTreeItemViewModel>> TreeItems { get; }
}
