using System.Collections.Generic;
using System.Threading.Tasks;
using ErsatzTV.Core.Plex;
using LanguageExt;

namespace ErsatzTV.Core.Interfaces.Plex
{
    public interface IPlexSecretStore
    {
        public Task<string> GetClientIdentifier();
        public Task<List<PlexUserAuthToken>> GetUserAuthTokens();
        public Task<Unit> UpsertUserAuthToken(PlexUserAuthToken userAuthToken);
        public Task<List<PlexServerAuthToken>> GetServerAuthTokens();
        public Task<Option<PlexServerAuthToken>> GetServerAuthToken(string clientIdentifier);
        public Task<Unit> UpsertServerAuthToken(PlexServerAuthToken serverAuthToken);
    }
}
