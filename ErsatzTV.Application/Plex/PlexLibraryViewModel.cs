using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.Plex
{
    public record PlexLibraryViewModel(int Id, string Name, LibraryMediaKind MediaKind, bool ShouldSyncItems);
}
