namespace ErsatzTV.Application.MediaItems.Commands
{
    public record RefreshMediaItemStatistics : RefreshMediaItem
    {
        public RefreshMediaItemStatistics(int mediaItemId) : base(mediaItemId)
        {
        }
    }
}
