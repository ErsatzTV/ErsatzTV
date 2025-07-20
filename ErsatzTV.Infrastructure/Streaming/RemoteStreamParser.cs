using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Streaming;

namespace ErsatzTV.Infrastructure.Streaming;

public class RemoteStreamParser(ILocalFileSystem localFileSystem) : IRemoteStreamParser
{
    public async Task<string> ParseRemoteStream(string path)
    {
        string allText = await localFileSystem.ReadAllText(path);
        return await allText
            .Split("\n")
            .Map(line => line.Trim())
            .Filter(line => !line.StartsWith("#", StringComparison.OrdinalIgnoreCase))
            .HeadOrNone()
            .IfNoneAsync(path);
    }
}
