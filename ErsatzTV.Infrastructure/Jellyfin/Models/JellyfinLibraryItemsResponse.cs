﻿namespace ErsatzTV.Infrastructure.Jellyfin.Models;

public class JellyfinLibraryItemsResponse
{
    public List<JellyfinLibraryItemResponse> Items { get; set; }
    public int TotalRecordCount { get; set; }
}
