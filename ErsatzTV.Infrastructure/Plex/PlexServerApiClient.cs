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
        public async Task<Either<BaseError, List<PlexLibrary>>> GetLibraries(
            PlexConnection connection,
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

        public async Task<Either<BaseError, List<PlexMovie>>> GetLibraryContents(
            PlexLibrary library,
            PlexConnection connection,
            PlexServerAuthToken token)
        {
            try
            {
                IPlexServerApi service = RestService.For<IPlexServerApi>(connection.Uri);
                return await service.GetLibrarySectionContents(library.Key, token.AuthToken)
                    .Map(r => r.MediaContainer.Metadata.Filter(m => m.Media.Count > 0 && m.Media[0].Part.Count > 0))
                    .Map(list => list.Map(ProjectToMovie).ToList());
            }
            catch (Exception ex)
            {
                return BaseError.New(ex.Message);
            }
        }

        private static Option<PlexLibrary> Project(PlexLibraryResponse response) =>
            response.Type switch
            {
                "show" => new PlexLibrary
                {
                    Key = response.Key,
                    Name = response.Title,
                    MediaKind = LibraryMediaKind.Shows,
                    ShouldSyncItems = false,
                    Paths = new List<LibraryPath> { new() { Path = $"plex://{response.Uuid}" } }
                },
                "movie" => new PlexLibrary
                {
                    Key = response.Key,
                    Name = response.Title,
                    MediaKind = LibraryMediaKind.Movies,
                    ShouldSyncItems = false,
                    Paths = new List<LibraryPath> { new() { Path = $"plex://{response.Uuid}" } }
                },
                // TODO: "artist" for music libraries
                _ => None
            };

        private static PlexPartEntry Project(PlexPartResponse response) =>
            new()
            {
                Id = response.Id,
                Key = response.Key,
                Duration = response.Duration,
                File = response.File,
                Size = response.Size
            };

        private static PlexMediaEntry Project(PlexMediaResponse response) =>
            new()
            {
                Id = response.Id,
                Duration = response.Duration,
                Bitrate = response.Bitrate,
                Width = response.Width,
                Height = response.Height,
                AspectRatio = response.AspectRatio,
                AudioChannels = response.AudioChannels,
                AudioCodec = response.AudioCodec,
                VideoCodec = response.VideoCodec,
                Container = response.Container,
                VideoFrameRate = response.VideoFrameRate,
                Part = response.Part.Map(Project).ToList()
            };

        private static PlexMetadataEntry Project(PlexMetadataResponse response) =>
            new()
            {
                Key = response.Key,
                Title = response.Title,
                Summary = response.Summary,
                Year = response.Year,
                Tagline = response.Tagline,
                Thumb = response.Thumb,
                Art = response.Art,
                AddedAt = response.AddedAt,
                UpdatedAt = response.UpdatedAt,
                Media = response.Media.Map(Project).ToList()
            };

        private static PlexMovie ProjectToMovie(PlexMetadataResponse response)
        {
            PlexMediaResponse media = response.Media.Head();
            PlexPartResponse part = media.Part.Head();
            DateTime lastWriteTime = DateTimeOffset.FromUnixTimeSeconds(response.UpdatedAt).DateTime;

            var metadata = new MovieMetadata
            {
                Title = response.Title,
                Plot = response.Summary,
                ReleaseDate = DateTime.Parse(response.OriginallyAvailableAt),
                Year = response.Year,
                Tagline = response.Tagline,
                DateAdded = DateTimeOffset.FromUnixTimeSeconds(response.AddedAt).DateTime,
                DateUpdated = lastWriteTime
            };

            // TODO: artwork

            var version = new MediaVersion
            {
                Name = "Main",
                Duration = TimeSpan.FromMilliseconds(media.Duration),
                Width = media.Width,
                Height = media.Height,
                AudioCodec = media.AudioCodec,
                VideoCodec = media.VideoCodec,
                // TODO: aspect ratio
                MediaFiles = new List<MediaFile>
                {
                    new PlexMediaFile
                    {
                        PlexId = part.Id,
                        Key = part.Key,
                        Path = part.File
                    }
                }
            };

            var movie = new PlexMovie
            {
                Key = response.Key,
                MovieMetadata = new List<MovieMetadata> { metadata },
                MediaVersions = new List<MediaVersion> { version }
            };

            return movie;
        }
    }
}
