using ErsatzTV.Application.Scheduling;
using MudBlazor;

namespace ErsatzTV.ViewModels;

public class BlockTreeItemViewModel
{
    public BlockTreeItemViewModel(BlockGroupViewModel blockGroup)
    {
        Text = blockGroup.Name;
        EndText = string.Empty;
        TreeItems = [];
        BlockGroupId = blockGroup.Id;
        Icon = Icons.Material.Filled.Folder;
    }

    public BlockTreeItemViewModel(BlockTreeBlockGroupViewModel blockGroup)
    {
        Text = blockGroup.Name;
        EndText = string.Empty;
        TreeItems = blockGroup.Blocks.Map(b => new TreeItemData<BlockTreeItemViewModel>
            { Value = new BlockTreeItemViewModel(b) }).ToList();
        BlockGroupId = blockGroup.Id;
        Icon = Icons.Material.Filled.Folder;
    }

    public BlockTreeItemViewModel(BlockViewModel block)
    {
        Text = block.Name;
        if (block.Minutes / 60 >= 1)
        {
            string plural = block.Minutes / 60 >= 2 ? "s" : string.Empty;
            EndText = $"{block.Minutes / 60} hour{plural}";
            if (block.Minutes % 60 != 0)
            {
                EndText += $", {block.Minutes % 60} minutes";
            }
        }
        else
        {
            EndText = $"{block.Minutes} minutes";
        }

        TreeItems = [];
        CanExpand = false;
        BlockId = block.Id;
    }

    public BlockTreeItemViewModel(BlockTreeBlockViewModel block)
    {
        Text = block.Name;
        if (block.Minutes / 60 >= 1)
        {
            string plural = block.Minutes / 60 >= 2 ? "s" : string.Empty;
            EndText = $"{block.Minutes / 60} hour{plural}";
            if (block.Minutes % 60 != 0)
            {
                EndText += $", {block.Minutes % 60} minutes";
            }
        }
        else
        {
            EndText = $"{block.Minutes} minutes";
        }

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

    public List<TreeItemData<BlockTreeItemViewModel>> TreeItems { get; }
}
