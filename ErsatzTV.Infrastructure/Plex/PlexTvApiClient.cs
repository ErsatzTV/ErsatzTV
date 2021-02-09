using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Plex;
using ErsatzTV.Core.Plex;
using ErsatzTV.Infrastructure.Plex.Models;
using LanguageExt;

namespace ErsatzTV.Infrastructure.Plex
{
    public class PlexTvApiClient : IPlexTvApiClient
    {
        private const string AppName = "ErsatzTV";
        private readonly IPlexSecretStore _plexSecretStore;

        private readonly IPlexTvApi _plexTvApi;

        public PlexTvApiClient(IPlexTvApi plexTvApi, IPlexSecretStore plexSecretStore)
        {
            // var client = new HttpClient(new HttpLoggingHandler()) { BaseAddress = new Uri("https://plex.tv/api/v2") };

            _plexTvApi = plexTvApi; // RestService.For<IPlexTvApi>(client);
            _plexSecretStore = plexSecretStore;
        }

        public async Task<Either<BaseError, List<PlexMediaSource>>> GetServers()
        {
            try
            {
                var result = new List<PlexMediaSource>();
                string clientIdentifier = await _plexSecretStore.GetClientIdentifier();
                foreach (PlexUserAuthToken token in await _plexSecretStore.GetUserAuthTokens())
                {
                    List<PlexResource> resources = await _plexTvApi.GetResources(clientIdentifier, token.AuthToken);
                    IEnumerable<PlexMediaSource> sources = resources
                        .Filter(r => r.Provides.Split(",").Any(p => p == "server"))
                        .Filter(r => r.Owned) // TODO: maybe support non-owned servers in the future
                        .Map(
                            resource =>
                            {
                                var serverAuthToken = new PlexServerAuthToken(
                                    resource.ClientIdentifier,
                                    resource.AccessToken);

                                _plexSecretStore.UpsertServerAuthToken(serverAuthToken);

                                var source = new PlexMediaSource
                                {
                                    Name = resource.Name,
                                    ProductVersion = resource.ProductVersion,
                                    ClientIdentifier = resource.ClientIdentifier,
                                    Connections = resource.Connections
                                        .Map(c => new PlexMediaSourceConnection { Uri = c.Uri }).ToList()
                                };

                                return source;
                            });
                    result.AddRange(sources);
                }

                return result;
            }
            catch (Exception ex)
            {
                return BaseError.New(ex.Message);
            }
        }

        public async Task<Either<BaseError, PlexAuthPin>> StartPinFlow()
        {
            try
            {
                string clientIdentifier = await _plexSecretStore.GetClientIdentifier();
                PlexPinResponse pinResponse = await _plexTvApi.StartPinFlow(AppName, clientIdentifier);
                return new PlexAuthPin(pinResponse.Id, pinResponse.Code, clientIdentifier);
            }
            catch (Exception ex)
            {
                return BaseError.New(ex.Message);
            }
        }

        public async Task<bool> TryCompletePinFlow(PlexAuthPin authPin)
        {
            try
            {
                PlexTokenResponse response = await _plexTvApi.GetPinStatus(
                    authPin.Id,
                    authPin.Code,
                    authPin.ClientIdentifier);

                if (!string.IsNullOrWhiteSpace(response.AuthToken))
                {
                    PlexUserResponse user = await _plexTvApi.GetUser(
                        AppName,
                        authPin.ClientIdentifier,
                        response.AuthToken);

                    var token = new PlexUserAuthToken(user.Email, user.AuthToken);
                    await _plexSecretStore.UpsertUserAuthToken(token);

                    return true;
                }
            }
            catch (Exception ex)
            {
                // ignored
            }

            return false;
        }
    }
}
