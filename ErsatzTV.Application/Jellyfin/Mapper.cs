using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.Jellyfin;

internal static class Mapper
{
    internal static JellyfinMediaSourceViewModel ProjectToViewModel(JellyfinMediaSource jellyfinMediaSource) =>
        new(
            jellyfinMediaSource.Id,
            jellyfinMediaSource.ServerName,
            jellyfinMediaSource.Connections.HeadOrNone().Match(c => c.Address, string.Empty));

    internal static JellyfinLibraryViewModel ProjectToViewModel(JellyfinLibrary library) =>
        new(library.Id, library.Name, library.MediaKind, library.ShouldSyncItems);

    internal static JellyfinPathReplacementViewModel ProjectToViewModel(JellyfinPathReplacement pathReplacement) =>
        new(pathReplacement.Id, pathReplacement.JellyfinPath, pathReplacement.LocalPath);
}
