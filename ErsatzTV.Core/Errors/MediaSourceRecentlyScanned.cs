namespace ErsatzTV.Core.Errors
{
    public class MediaSourceRecentlyScanned : BaseError
    {
        public MediaSourceRecentlyScanned(string folder) :
            base($"Media source {folder} was already scanned recently; skipping scan.")
        {
        }
    }
}
