namespace ErsatzTV.Core.Scheduling;

public class PlayoutBuildException : Exception
{
    public PlayoutBuildException() : base() { }
    public PlayoutBuildException(string message) : base(message) { }
    public PlayoutBuildException(string message, Exception innerException) : base(message, innerException) { }
}
