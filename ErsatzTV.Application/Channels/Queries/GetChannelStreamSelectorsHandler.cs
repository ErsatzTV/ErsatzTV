using ErsatzTV.Core;
using ErsatzTV.Core.Interfaces.Metadata;

namespace ErsatzTV.Application.Channels;

public class GetChannelStreamSelectorsHandler(ILocalFileSystem localFileSystem)
    : IRequestHandler<GetChannelStreamSelectors, List<string>>
{
    public Task<List<string>> Handle(GetChannelStreamSelectors request, CancellationToken cancellationToken) =>
        localFileSystem.ListFiles(FileSystemLayout.ChannelStreamSelectorsFolder)
            .Map(Path.GetFileNameWithoutExtension)
            .ToList()
            .AsTask();
}
