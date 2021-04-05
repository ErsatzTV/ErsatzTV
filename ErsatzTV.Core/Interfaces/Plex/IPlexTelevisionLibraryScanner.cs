using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Plex;
using LanguageExt;

namespace ErsatzTV.Core.Interfaces.Plex
{
    public interface IPlexTelevisionLibraryScanner
    {
        Task<Either<BaseError, Unit>> ScanLibrary(
            PlexConnection connection,
            PlexServerAuthToken token,
            PlexLibrary plexMediaSourceLibrary,
            Func<List<MediaItem>, ValueTask> addToSearchIndex,
            Func<List<int>, ValueTask> removeFromSearchIndex);
    }
}
