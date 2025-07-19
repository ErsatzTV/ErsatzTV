using ErsatzTV.Application.MediaCollections;
using ErsatzTV.Application.Tree;
using MudBlazor;

namespace ErsatzTV.ViewModels;

public class PlaylistTreeItemViewModel
{
    public PlaylistTreeItemViewModel(PlaylistGroupViewModel playlistGroup)
    {
        Text = playlistGroup.Name;
        EndText = string.Empty;
        TreeItems = [];
        PlaylistGroupId = playlistGroup.Id;
        Icon = Icons.Material.Filled.Folder;
        IsSystem = playlistGroup.IsSystem;
    }

    public PlaylistTreeItemViewModel(TreeGroupViewModel playlistGroup)
    {
        Text = playlistGroup.Name;
        EndText = string.Empty;
        TreeItems = playlistGroup.Children.Map(p => new TreeItemData<PlaylistTreeItemViewModel>
            { Value = new PlaylistTreeItemViewModel(p) }).ToList();
        PlaylistGroupId = playlistGroup.Id;
        Icon = Icons.Material.Filled.Folder;
        IsSystem = playlistGroup.IsSystem;
    }

    public PlaylistTreeItemViewModel(PlaylistViewModel playlist)
    {
        Text = playlist.Name;
        TreeItems = [];
        CanExpand = false;
        PlaylistId = playlist.Id;
        IsSystem = playlist.IsSystem;
    }

    public PlaylistTreeItemViewModel(TreeItemViewModel playlist)
    {
        Text = playlist.Name;
        TreeItems = [];
        CanExpand = false;
        PlaylistId = playlist.Id;
        IsSystem = playlist.IsSystem;
    }

    public string Text { get; }

    public string EndText { get; }

    public string Icon { get; }

    public bool CanExpand { get; }

    public int? PlaylistId { get; }

    public int? PlaylistGroupId { get; }

    public bool IsSystem { get; }

    public List<TreeItemData<PlaylistTreeItemViewModel>> TreeItems { get; }
}
