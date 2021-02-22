namespace ErsatzTV.Core.Errors
{
    public class MediaSourceInaccessible : BaseError
    {
        public MediaSourceInaccessible()
            : base("Media source is not accessible or missing")
        {
        }
    }
}
