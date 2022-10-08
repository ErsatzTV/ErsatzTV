using ErsatzTV.Core;
using ErsatzTV.Core.Interfaces.Metadata;

namespace ErsatzTV.Application.Templates;

public class GetMusicVideoCreditTemplatesHandler : IRequestHandler<GetMusicVideoCreditTemplates, List<string>>
{
    private readonly ILocalFileSystem _localFileSystem;

    public GetMusicVideoCreditTemplatesHandler(ILocalFileSystem localFileSystem)
    {
        _localFileSystem = localFileSystem;
    }

    public Task<List<string>> Handle(GetMusicVideoCreditTemplates request, CancellationToken cancellationToken) =>
        _localFileSystem.ListFiles(FileSystemLayout.MusicVideoCreditsTemplatesFolder)
            .Map(Path.GetFileNameWithoutExtension)
            .ToList()
            .AsTask();
}
