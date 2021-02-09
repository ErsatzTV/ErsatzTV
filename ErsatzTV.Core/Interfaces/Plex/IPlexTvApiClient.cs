using System.Collections.Generic;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Plex;
using LanguageExt;

namespace ErsatzTV.Core.Interfaces.Plex
{
    public interface IPlexTvApiClient
    {
        Task<Either<BaseError, PlexAuthPin>> StartPinFlow();
        Task<bool> TryCompletePinFlow(PlexAuthPin authPin);
        Task<Either<BaseError, List<PlexMediaSource>>> GetServers();
    }
}
