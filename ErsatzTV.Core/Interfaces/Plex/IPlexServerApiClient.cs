using System.Collections.Generic;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Plex;
using LanguageExt;

namespace ErsatzTV.Core.Interfaces.Plex
{
    public interface IPlexServerApiClient
    {
        Task<Either<BaseError, List<PlexMediaSourceLibrary>>> GetLibraries(
            PlexMediaSourceConnection connection,
            PlexServerAuthToken token);
    }
}
