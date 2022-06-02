namespace ErsatzTV.Infrastructure.Emby.Models;

public class EmbyLibraryItemsResponse
{
    public List<EmbyLibraryItemResponse> Items { get; set; }
    public int TotalRecordCount { get; set; }
}
