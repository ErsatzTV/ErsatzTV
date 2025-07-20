namespace ErsatzTV.Core.Interfaces.Streaming;

public interface IRemoteStreamParser
{
    public Task<string> ParseRemoteStream(string path);
}
