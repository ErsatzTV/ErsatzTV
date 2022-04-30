namespace ErsatzTV.Core.Errors;

public class FailedToReadNfo : BaseError
{
    public FailedToReadNfo(string message = null) : base(
        string.IsNullOrWhiteSpace(message) ? "Failed to read NFO metadata" : $"Failed to read NFO metadata: {message}")
    {
    }
}
