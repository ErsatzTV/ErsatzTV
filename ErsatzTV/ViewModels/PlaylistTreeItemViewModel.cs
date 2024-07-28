using ErsatzTV.Application.MediaCollections;
using MudBlazor;
using S = System.Collections.Generic;

namespace ErsatzTV.ViewModels;

public class PlaylistTreeItemViewModel
{
    public PlaylistTreeItemViewModel(PlaylistGroupViewModel playlistGroup)
    {
        Text = playlistGroup.Name;
        EndText = string.Empty;
        TreeItems = [];
        CanExpand = playlistGroup.PlaylistCount > 0;
        PlaylistGroupId = playlistGroup.Id;
        Icon = Icons.Material.Filled.Folder;
    }

    public PlaylistTreeItemViewModel(PlaylistViewModel playlist)
    {
        Text = playlist.Name;
        TreeItems = [];
        CanExpand = false;
        PlaylistId = playlist.Id;
    }

    public string Text { get; }

    public string EndText { get; }

    public string Icon { get; }

    public bool CanExpand { get; }

    public int? PlaylistId { get; }

    public int? PlaylistGroupId { get; }

    public List<TreeItemData<PlaylistTreeItemViewModel>> TreeItems { get; }
}
