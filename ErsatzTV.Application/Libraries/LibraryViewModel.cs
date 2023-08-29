using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.Libraries;

public record LibraryViewModel(
    string LibraryKind,
    int Id,
    string Name,
    LibraryMediaKind MediaKind,
    int MediaSourceId,
    string MediaSourceName);
