using System;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.Libraries
{
    internal static class Mapper
    {
        public static LibraryViewModel ProjectToViewModel(Library library) =>
            library switch
            {
                LocalLibrary l => ProjectToViewModel(l),
                PlexLibrary p => new PlexLibraryViewModel(p.Id, p.Name, p.MediaKind),
                _ => throw new ArgumentOutOfRangeException(nameof(library))
            };

        public static LocalLibraryViewModel ProjectToViewModel(LocalLibrary library) =>
            new(library.Id, library.Name, library.MediaKind);

        public static LocalLibraryPathViewModel ProjectToViewModel(LibraryPath libraryPath) =>
            new(libraryPath.Id, libraryPath.LibraryId, libraryPath.Path);
    }
}
