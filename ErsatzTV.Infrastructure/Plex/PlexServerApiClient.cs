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
using Refit;
using static LanguageExt.Prelude;

namespace ErsatzTV.Infrastructure.Plex
{
    public class PlexServerApiClient : IPlexServerApiClient
    {
        public async Task<Either<BaseError, List<PlexMediaSourceLibrary>>> GetLibraries(
            PlexMediaSourceConnection connection,
            PlexServerAuthToken token)
        {
            try
            {
                IPlexServerApi service = RestService.For<IPlexServerApi>(connection.Uri);
                List<PlexLibraryResponse> directory =
                    await service.GetLibraries(token.AuthToken).Map(r => r.MediaContainer.Directory);
                return directory
                    .Filter(l => l.Hidden == 0)
                    .Map(Project)
                    .Somes()
                    .ToList();
            }
            catch (Exception ex)
            {
                return BaseError.New(ex.Message);
            }
        }

        private static Option<PlexMediaSourceLibrary> Project(PlexLibraryResponse response) =>
            response.Type switch
            {
                "show" => new PlexMediaSourceLibrary
                {
                    Key = response.Key,
                    Name = response.Title,
                    MediaType = MediaType.TvShow
                },
                "movie" => new PlexMediaSourceLibrary
                {
                    Key = response.Key,
                    Name = response.Title,
                    MediaType = MediaType.Movie
                },
                // TODO: "artist" for music libraries
                _ => None
            };
    }
}
