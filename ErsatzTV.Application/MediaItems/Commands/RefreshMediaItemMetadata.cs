namespace ErsatzTV.Application.MediaItems.Commands
{
    public record RefreshMediaItemMetadata : RefreshMediaItem
    {
        public RefreshMediaItemMetadata(int mediaItemId) : base(mediaItemId)
        {
        }
    }
}
