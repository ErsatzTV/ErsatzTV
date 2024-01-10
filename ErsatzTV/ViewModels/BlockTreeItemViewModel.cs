using ErsatzTV.Application.Scheduling;
using MudBlazor;
using S = System.Collections.Generic;

namespace ErsatzTV.ViewModels;

public class BlockTreeItemViewModel
{
    public BlockTreeItemViewModel(BlockGroupViewModel blockGroup)
    {
        Text = blockGroup.Name;
        EndText = string.Empty;
        TreeItems = [];
        CanExpand = blockGroup.BlockCount > 0;
        BlockGroupId = blockGroup.Id;
        Icon = Icons.Material.Filled.Folder;
    }

    public BlockTreeItemViewModel(BlockViewModel block)
    {
        Text = block.Name;
        EndText = $"{block.Minutes} minutes";
        TreeItems = [];
        CanExpand = false;
        BlockId = block.Id;
    }
    
    public string Text { get; }
    
    public string EndText { get; }
    
    public string Icon { get; }
    
    public bool CanExpand { get; }
    
    public int? BlockId { get; }
    
    public int? BlockGroupId { get; }
    
    public S.HashSet<BlockTreeItemViewModel> TreeItems { get; }
}
