using ErsatzTV.Application.Libraries;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.Emby;

public record EmbyLibraryViewModel(
        int Id,
        string Name,
        LibraryMediaKind MediaKind,
        bool ShouldSyncItems,
        int MediaSourceId)
    : LibraryViewModel("Emby", Id, Name, MediaKind, MediaSourceId, string.Empty);
