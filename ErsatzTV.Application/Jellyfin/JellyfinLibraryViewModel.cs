using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.Jellyfin
{
    public record JellyfinLibraryViewModel(int Id, string Name, LibraryMediaKind MediaKind, bool ShouldSyncItems);
}
