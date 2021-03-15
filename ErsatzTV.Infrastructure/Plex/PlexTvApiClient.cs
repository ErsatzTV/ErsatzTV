﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Plex;
using ErsatzTV.Core.Plex;
using ErsatzTV.Infrastructure.Plex.Models;
using LanguageExt;
using Refit;

namespace ErsatzTV.Infrastructure.Plex
{
    public class PlexTvApiClient : IPlexTvApiClient
    {
        private const string AppName = "ErsatzTV";
        private readonly IPlexSecretStore _plexSecretStore;

        private readonly IPlexTvApi _plexTvApi;

        public PlexTvApiClient(IPlexTvApi plexTvApi, IPlexSecretStore plexSecretStore)
        {
            _plexTvApi = plexTvApi;
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
                    List<PlexResource> httpResources = await _plexTvApi.GetResources(
                        0,
                        clientIdentifier,
                        token.AuthToken);

                    List<PlexResource> httpsResources = await _plexTvApi.GetResources(
                        1,
                        clientIdentifier,
                        token.AuthToken);


                    var allResources = httpResources.Filter(resource => resource.HttpsRequired == false)
                        .Append(httpsResources.Filter(resource => resource.HttpsRequired))
                        .ToList();

                    IEnumerable<PlexMediaSource> sources = allResources
                        .Filter(r => r.Provides.Split(",").Any(p => p == "server"))
                        .Filter(r => r.Owned) // TODO: maybe support non-owned servers in the future
                        .Map(
                            resource =>
                            {
                                var serverAuthToken = new PlexServerAuthToken(
                                    resource.ClientIdentifier,
                                    resource.AccessToken);

                                _plexSecretStore.UpsertServerAuthToken(serverAuthToken);
                                List<PlexResourceConnection> sortedConnections = resource.HttpsRequired
                                    ? resource.Connections
                                    : resource.Connections.OrderBy(c => c.Local ? 0 : 1).ToList();

                                var source = new PlexMediaSource
                                {
                                    ServerName = resource.Name,
                                    ProductVersion = resource.ProductVersion,
                                    ClientIdentifier = resource.ClientIdentifier,
                                    Connections = sortedConnections
                                        .Map(c => new PlexConnection { Uri = c.Uri }).ToList()
                                };

                                return source;
                            });
                    result.AddRange(sources);
                }

                return result;
            }
            catch (ApiException apiException)
            {
                if (apiException.ReasonPhrase == "Unauthorized")
                {
                    await _plexSecretStore.DeleteAll();
                }
                
                return BaseError.New(apiException.Message);
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
            catch (Exception)
            {
                // ignored
            }

            return false;
        }
    }
}
