using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.Libraries;

public record LocalLibraryViewModel(int Id, string Name, LibraryMediaKind MediaKind, int MediaSourceId)
    : LibraryViewModel("Local", Id, Name, MediaKind, MediaSourceId, string.Empty);
