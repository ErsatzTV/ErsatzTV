﻿using ErsatzTV.Application.Emby;
using ErsatzTV.Application.Jellyfin;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.Libraries;

internal static class Mapper
{
    public static LibraryViewModel ProjectToViewModel(Library library) =>
        library switch
        {
            LocalLibrary l => ProjectToViewModel(l),
            PlexLibrary p => new PlexLibraryViewModel(p.Id, p.Name, p.MediaKind, p.MediaSourceId),
            JellyfinLibrary j => new JellyfinLibraryViewModel(
                j.Id,
                j.Name,
                j.MediaKind,
                j.ShouldSyncItems,
                j.MediaSourceId),
            EmbyLibrary e => new EmbyLibraryViewModel(e.Id, e.Name, e.MediaKind, e.ShouldSyncItems, e.MediaSourceId),
            _ => throw new ArgumentOutOfRangeException(nameof(library))
        };

    public static LocalLibraryViewModel ProjectToViewModel(LocalLibrary library) =>
        new(library.Id, library.Name, library.MediaKind, library.MediaSourceId);

    public static LocalLibraryPathViewModel ProjectToViewModel(LibraryPath libraryPath) =>
        new(libraryPath.Id, libraryPath.LibraryId, libraryPath.Path);
}
