using ErsatzTV.Application.Scheduling;
using ErsatzTV.Application.Tree;
using MudBlazor;

namespace ErsatzTV.ViewModels;

public class DecoTreeItemViewModel
{
    public DecoTreeItemViewModel(DecoGroupViewModel decoGroup)
    {
        Text = decoGroup.Name;
        EndText = string.Empty;
        TreeItems = [];
        DecoGroupId = decoGroup.Id;
        Icon = Icons.Material.Filled.Folder;
    }

    public DecoTreeItemViewModel(TreeGroupViewModel decoGroup)
    {
        Text = decoGroup.Name;
        TreeItems = decoGroup.Children.Map(d => new TreeItemData<DecoTreeItemViewModel>
            { Value = new DecoTreeItemViewModel(d) }).ToList();
        DecoGroupId = decoGroup.Id;
        Icon = Icons.Material.Filled.Folder;
    }

    public DecoTreeItemViewModel(DecoViewModel deco)
    {
        Text = deco.Name;
        TreeItems = [];
        CanExpand = false;
        DecoId = deco.Id;
    }

    public DecoTreeItemViewModel(TreeItemViewModel deco)
    {
        Text = deco.Name;
        TreeItems = [];
        CanExpand = false;
        DecoId = deco.Id;
    }

    public string Text { get; }

    public string EndText { get; }

    public string Icon { get; }

    public bool CanExpand { get; }

    public int? DecoId { get; }

    public int? DecoGroupId { get; }

    public List<TreeItemData<DecoTreeItemViewModel>> TreeItems { get; }
}
