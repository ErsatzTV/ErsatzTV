namespace ErsatzTV.ViewModels;

public class PlaylistItemsEditViewModel
{
    public string Name { get; set; }
    public bool IsSystem { get; set; }
    public List<PlaylistItemEditViewModel> Items { get; set; }
}
