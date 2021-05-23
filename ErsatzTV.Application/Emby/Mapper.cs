using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.Emby
{
    internal static class Mapper
    {
        internal static EmbyMediaSourceViewModel ProjectToViewModel(EmbyMediaSource embyMediaSource) =>
            new(
                embyMediaSource.Id,
                embyMediaSource.ServerName,
                embyMediaSource.Connections.HeadOrNone().Match(c => c.Address, string.Empty));

        internal static EmbyLibraryViewModel ProjectToViewModel(EmbyLibrary library) =>
            new(library.Id, library.Name, library.MediaKind, library.ShouldSyncItems);

        internal static EmbyPathReplacementViewModel ProjectToViewModel(EmbyPathReplacement pathReplacement) =>
            new(pathReplacement.Id, pathReplacement.EmbyPath, pathReplacement.LocalPath);
    }
}
