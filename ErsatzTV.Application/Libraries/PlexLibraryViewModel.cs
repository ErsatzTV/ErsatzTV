using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.Libraries;

public record PlexLibraryViewModel(int Id, string Name, LibraryMediaKind MediaKind, int MediaSourceId)
    : LibraryViewModel("Plex", Id, Name, MediaKind, MediaSourceId);
