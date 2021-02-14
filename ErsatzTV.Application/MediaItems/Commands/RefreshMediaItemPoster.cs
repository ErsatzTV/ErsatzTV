namespace ErsatzTV.Application.MediaItems.Commands
{
    public record RefreshMediaItemPoster : RefreshMediaItem
    {
        public RefreshMediaItemPoster(int mediaItemId) : base(mediaItemId)
        {
        }
    }
}
