using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Metadata;

public class MediaItemScanResult<T> where T : MediaItem
{
    public MediaItemScanResult(T item) => Item = item;

    public T Item { get; set; }
    public string LocalPath { get; set; }

    public bool IsAdded { get; set; }
    public bool IsUpdated { get; set; }
}
