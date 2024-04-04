using ErsatzTV.Application.Scheduling;
using MudBlazor;
using S = System.Collections.Generic;

namespace ErsatzTV.ViewModels;

public class DecoTreeItemViewModel
{
    public DecoTreeItemViewModel(DecoGroupViewModel decoGroup)
    {
        Text = decoGroup.Name;
        EndText = string.Empty;
        TreeItems = [];
        CanExpand = decoGroup.DecoCount > 0;
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

    public string Text { get; }

    public string EndText { get; }

    public string Icon { get; }

    public bool CanExpand { get; }

    public int? DecoId { get; }

    public int? DecoGroupId { get; }

    public S.HashSet<DecoTreeItemViewModel> TreeItems { get; }
}
