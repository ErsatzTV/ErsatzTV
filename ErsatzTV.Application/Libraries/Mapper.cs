using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.Libraries
{
    internal static class Mapper
    {
        public static LocalLibraryViewModel ProjectToViewModel(LocalLibrary library) =>
            new(library.Id, library.Name, library.MediaKind);

        public static LocalLibraryPathViewModel ProjectToViewModel(LibraryPath libraryPath) =>
            new(libraryPath.Id, libraryPath.LibraryId, libraryPath.Path);
    }
}
