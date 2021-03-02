using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.Libraries
{
    public record LocalLibraryViewModel(int Id, string Name, LibraryMediaKind MediaKind)
        : LibraryViewModel("Local", Id, Name, MediaKind);
}
