using ErsatzTV.Core.Domain;

namespace ErsatzTV.Scanner.Core.Interfaces.Metadata;

public interface ILocalChaptersProvider : IDisposable
{
    Task<bool> UpdateChapters(MediaItem mediaItem, Option<string> localPath, CancellationToken cancellationToken);
}
