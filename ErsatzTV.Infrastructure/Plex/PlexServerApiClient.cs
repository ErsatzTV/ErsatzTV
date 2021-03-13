using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Metadata;
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
        private readonly IFallbackMetadataProvider _fallbackMetadataProvider;

        public PlexServerApiClient(IFallbackMetadataProvider fallbackMetadataProvider) =>
            _fallbackMetadataProvider = fallbackMetadataProvider;

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
                    .Map(list => list.Map(metadata => ProjectToMovie(metadata, library.MediaSourceId)).ToList());
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

        private PlexMovie ProjectToMovie(PlexMetadataResponse response, int mediaSourceId)
        {
            PlexMediaResponse media = response.Media.Head();
            PlexPartResponse part = media.Part.Head();
            DateTime dateAdded = DateTimeOffset.FromUnixTimeSeconds(response.AddedAt).DateTime;
            DateTime lastWriteTime = DateTimeOffset.FromUnixTimeSeconds(response.UpdatedAt).DateTime;

            var metadata = new MovieMetadata
            {
                Title = response.Title,
                SortTitle = _fallbackMetadataProvider.GetSortTitle(response.Title),
                Plot = response.Summary,
                ReleaseDate = DateTime.Parse(response.OriginallyAvailableAt),
                Year = response.Year,
                Tagline = response.Tagline,
                DateAdded = dateAdded,
                DateUpdated = lastWriteTime,
                Genres = response.Genre.Map(g => new Genre { Name = g.Tag }).ToList()
            };

            if (!string.IsNullOrWhiteSpace(response.Thumb))
            {
                var path = $"plex/{mediaSourceId}{response.Thumb}";
                var artwork = new Artwork
                {
                    ArtworkKind = ArtworkKind.Poster,
                    Path = path,
                    DateAdded = dateAdded,
                    DateUpdated = lastWriteTime
                };

                metadata.Artwork ??= new List<Artwork>();
                metadata.Artwork.Add(artwork);
            }

            if (!string.IsNullOrWhiteSpace(response.Art))
            {
                var path = $"plex/{mediaSourceId}{response.Art}";
                var artwork = new Artwork
                {
                    ArtworkKind = ArtworkKind.FanArt,
                    Path = path,
                    DateAdded = dateAdded,
                    DateUpdated = lastWriteTime
                };

                metadata.Artwork ??= new List<Artwork>();
                metadata.Artwork.Add(artwork);
            }

            var version = new MediaVersion
            {
                Name = "Main",
                Duration = TimeSpan.FromMilliseconds(media.Duration),
                Width = media.Width,
                Height = media.Height,
                AudioCodec = media.AudioCodec,
                VideoCodec = media.VideoCodec,
                VideoProfile = media.VideoProfile,
                SampleAspectRatio = ConvertToSAR(media.AspectRatio),
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

        private static string ConvertToSAR(double aspectRatio) => "1:1";
        // TODO: fix this with more detailed stats pull from plex for each item
        // Math.Abs(aspectRatio - 1) < 0.01 ? "1:1" : $"{(int) (aspectRatio * 100)}:100";
    }
}
