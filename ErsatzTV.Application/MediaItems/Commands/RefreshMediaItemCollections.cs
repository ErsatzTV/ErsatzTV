namespace ErsatzTV.Application.MediaItems.Commands
{
    public record RefreshMediaItemCollections : RefreshMediaItem
    {
        public RefreshMediaItemCollections(int mediaItemId) : base(mediaItemId)
        {
        }
    }
}
