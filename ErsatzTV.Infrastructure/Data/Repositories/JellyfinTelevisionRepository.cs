using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Jellyfin;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Infrastructure.Data.Repositories
{
    public class JellyfinTelevisionRepository : IJellyfinTelevisionRepository
    {
        private readonly IDbConnection _dbConnection;
        private readonly IDbContextFactory<TvContext> _dbContextFactory;

        public JellyfinTelevisionRepository(IDbConnection dbConnection, IDbContextFactory<TvContext> dbContextFactory)
        {
            _dbConnection = dbConnection;
            _dbContextFactory = dbContextFactory;
        }

        public Task<List<JellyfinItemEtag>> GetExistingShows(JellyfinLibrary library) =>
            _dbConnection.QueryAsync<JellyfinItemEtag>(
                    @"SELECT ItemId, Etag FROM JellyfinShow
                      INNER JOIN Show S on JellyfinShow.Id = S.Id
                      INNER JOIN MediaItem MI on S.Id = MI.Id
                      INNER JOIN LibraryPath LP on MI.LibraryPathId = LP.Id
                      WHERE LP.LibraryId = @LibraryId",
                    new { LibraryId = library.Id })
                .Map(result => result.ToList());

        public Task<List<JellyfinItemEtag>> GetExistingSeasons(JellyfinLibrary library, string showItemId) =>
            _dbConnection.QueryAsync<JellyfinItemEtag>(
                    @"SELECT JellyfinSeason.ItemId, JellyfinSeason.Etag FROM JellyfinSeason
                      INNER JOIN Season S on JellyfinSeason.Id = S.Id
                      INNER JOIN MediaItem MI on S.Id = MI.Id
                      INNER JOIN LibraryPath LP on MI.LibraryPathId = LP.Id
                      INNER JOIN Show S2 on S.ShowId = S2.Id
                      INNER JOIN JellyfinShow JS on S2.Id = JS.Id
                      WHERE LP.LibraryId = @LibraryId AND JS.ItemId = @ShowItemId",
                    new { LibraryId = library.Id, ShowItemId = showItemId })
                .Map(result => result.ToList());

        public Task<List<JellyfinItemEtag>> GetExistingEpisodes(JellyfinLibrary library, string seasonItemId) =>
            _dbConnection.QueryAsync<JellyfinItemEtag>(
                    @"SELECT JellyfinEpisode.ItemId, JellyfinEpisode.Etag FROM JellyfinEpisode
                      INNER JOIN Episode E on JellyfinEpisode.Id = E.Id
                      INNER JOIN MediaItem MI on E.Id = MI.Id
                      INNER JOIN LibraryPath LP on MI.LibraryPathId = LP.Id
                      INNER JOIN Season S2 on E.SeasonId = S2.Id
                      INNER JOIN JellyfinSeason JS on S2.Id = JS.Id
                      WHERE LP.LibraryId = @LibraryId AND JS.ItemId = @SeasonItemId",
                    new { LibraryId = library.Id, SeasonItemId = seasonItemId })
                .Map(result => result.ToList());

        public async Task<bool> AddShow(JellyfinShow show)
        {
            await using TvContext dbContext = _dbContextFactory.CreateDbContext();
            await dbContext.AddAsync(show);
            if (await dbContext.SaveChangesAsync() <= 0)
            {
                return false;
            }

            await dbContext.Entry(show).Reference(m => m.LibraryPath).LoadAsync();
            await dbContext.Entry(show.LibraryPath).Reference(lp => lp.Library).LoadAsync();
            return true;
        }

        public async Task<Unit> Update(JellyfinShow show)
        {
            await using TvContext dbContext = _dbContextFactory.CreateDbContext();
            Option<JellyfinShow> maybeExisting = await dbContext.JellyfinShows
                .Include(m => m.LibraryPath)
                .Include(m => m.ShowMetadata)
                .ThenInclude(mm => mm.Genres)
                .Include(m => m.ShowMetadata)
                .ThenInclude(mm => mm.Tags)
                .Include(m => m.ShowMetadata)
                .ThenInclude(mm => mm.Studios)
                .Include(m => m.ShowMetadata)
                .ThenInclude(mm => mm.Actors)
                .Include(m => m.ShowMetadata)
                .ThenInclude(mm => mm.Artwork)
                .Filter(m => m.ItemId == show.ItemId)
                .OrderBy(m => m.ItemId)
                .SingleOrDefaultAsync();

            if (maybeExisting.IsSome)
            {
                JellyfinShow existing = maybeExisting.ValueUnsafe();

                // library path is used for search indexing later
                show.LibraryPath = existing.LibraryPath;

                existing.Etag = show.Etag;

                // metadata
                ShowMetadata metadata = existing.ShowMetadata.Head();
                ShowMetadata incomingMetadata = show.ShowMetadata.Head();
                metadata.Title = incomingMetadata.Title;
                metadata.SortTitle = incomingMetadata.SortTitle;
                metadata.Plot = incomingMetadata.Plot;
                metadata.Year = incomingMetadata.Year;
                metadata.Tagline = incomingMetadata.Tagline;
                metadata.DateAdded = incomingMetadata.DateAdded;
                metadata.DateUpdated = DateTime.UtcNow;

                // genres
                foreach (Genre genre in metadata.Genres
                    .Filter(g => incomingMetadata.Genres.All(g2 => g2.Name != g.Name))
                    .ToList())
                {
                    metadata.Genres.Remove(genre);
                }

                foreach (Genre genre in incomingMetadata.Genres
                    .Filter(g => metadata.Genres.All(g2 => g2.Name != g.Name))
                    .ToList())
                {
                    metadata.Genres.Add(genre);
                }

                // tags
                foreach (Tag tag in metadata.Tags
                    .Filter(g => incomingMetadata.Tags.All(g2 => g2.Name != g.Name))
                    .ToList())
                {
                    metadata.Tags.Remove(tag);
                }

                foreach (Tag tag in incomingMetadata.Tags
                    .Filter(g => metadata.Tags.All(g2 => g2.Name != g.Name))
                    .ToList())
                {
                    metadata.Tags.Add(tag);
                }

                // studios
                foreach (Studio studio in metadata.Studios
                    .Filter(g => incomingMetadata.Studios.All(g2 => g2.Name != g.Name))
                    .ToList())
                {
                    metadata.Studios.Remove(studio);
                }

                foreach (Studio studio in incomingMetadata.Studios
                    .Filter(g => metadata.Studios.All(g2 => g2.Name != g.Name))
                    .ToList())
                {
                    metadata.Studios.Add(studio);
                }

                // actors
                foreach (Actor actor in metadata.Actors
                    .Filter(
                        a => incomingMetadata.Actors.All(
                            a2 => a2.Name != a.Name || a.Artwork == null && a2.Artwork != null))
                    .ToList())
                {
                    metadata.Actors.Remove(actor);
                }

                foreach (Actor actor in incomingMetadata.Actors
                    .Filter(a => metadata.Actors.All(a2 => a2.Name != a.Name))
                    .ToList())
                {
                    metadata.Actors.Add(actor);
                }

                metadata.ReleaseDate = incomingMetadata.ReleaseDate;

                // poster
                Artwork incomingPoster =
                    incomingMetadata.Artwork.FirstOrDefault(a => a.ArtworkKind == ArtworkKind.Poster);
                if (incomingPoster != null)
                {
                    Artwork poster = metadata.Artwork.FirstOrDefault(a => a.ArtworkKind == ArtworkKind.Poster);
                    if (poster == null)
                    {
                        poster = new Artwork { ArtworkKind = ArtworkKind.Poster };
                        metadata.Artwork.Add(poster);
                    }

                    poster.Path = incomingPoster.Path;
                    poster.DateAdded = incomingPoster.DateAdded;
                    poster.DateUpdated = incomingPoster.DateUpdated;
                }

                // fan art
                Artwork incomingFanArt =
                    incomingMetadata.Artwork.FirstOrDefault(a => a.ArtworkKind == ArtworkKind.FanArt);
                if (incomingFanArt != null)
                {
                    Artwork fanArt = metadata.Artwork.FirstOrDefault(a => a.ArtworkKind == ArtworkKind.FanArt);
                    if (fanArt == null)
                    {
                        fanArt = new Artwork { ArtworkKind = ArtworkKind.FanArt };
                        metadata.Artwork.Add(fanArt);
                    }

                    fanArt.Path = incomingFanArt.Path;
                    fanArt.DateAdded = incomingFanArt.DateAdded;
                    fanArt.DateUpdated = incomingFanArt.DateUpdated;
                }
            }

            await dbContext.SaveChangesAsync();

            return Unit.Default;
        }

        public async Task<bool> AddSeason(JellyfinSeason season)
        {
            await using TvContext dbContext = _dbContextFactory.CreateDbContext();
            await dbContext.AddAsync(season);
            if (await dbContext.SaveChangesAsync() <= 0)
            {
                return false;
            }

            await dbContext.Entry(season).Reference(m => m.LibraryPath).LoadAsync();
            await dbContext.Entry(season.LibraryPath).Reference(lp => lp.Library).LoadAsync();
            return true;
        }

        public async Task<Unit> Update(JellyfinSeason season)
        {
            await using TvContext dbContext = _dbContextFactory.CreateDbContext();
            Option<JellyfinSeason> maybeExisting = await dbContext.JellyfinSeasons
                .Include(m => m.LibraryPath)
                .Include(m => m.SeasonMetadata)
                .ThenInclude(mm => mm.Artwork)
                .Filter(m => m.ItemId == season.ItemId)
                .OrderBy(m => m.ItemId)
                .SingleOrDefaultAsync();

            if (maybeExisting.IsSome)
            {
                JellyfinSeason existing = maybeExisting.ValueUnsafe();

                // library path is used for search indexing later
                season.LibraryPath = existing.LibraryPath;

                existing.Etag = season.Etag;
                existing.SeasonNumber = season.SeasonNumber;

                // metadata
                SeasonMetadata metadata = existing.SeasonMetadata.Head();
                SeasonMetadata incomingMetadata = season.SeasonMetadata.Head();
                metadata.Title = incomingMetadata.Title;
                metadata.SortTitle = incomingMetadata.SortTitle;
                metadata.Year = incomingMetadata.Year;
                metadata.DateAdded = incomingMetadata.DateAdded;
                metadata.DateUpdated = DateTime.UtcNow;
                metadata.ReleaseDate = incomingMetadata.ReleaseDate;

                // poster
                Artwork incomingPoster =
                    incomingMetadata.Artwork.FirstOrDefault(a => a.ArtworkKind == ArtworkKind.Poster);
                if (incomingPoster != null)
                {
                    Artwork poster = metadata.Artwork.FirstOrDefault(a => a.ArtworkKind == ArtworkKind.Poster);
                    if (poster == null)
                    {
                        poster = new Artwork { ArtworkKind = ArtworkKind.Poster };
                        metadata.Artwork.Add(poster);
                    }

                    poster.Path = incomingPoster.Path;
                    poster.DateAdded = incomingPoster.DateAdded;
                    poster.DateUpdated = incomingPoster.DateUpdated;
                }

                // fan art
                Artwork incomingFanArt =
                    incomingMetadata.Artwork.FirstOrDefault(a => a.ArtworkKind == ArtworkKind.FanArt);
                if (incomingFanArt != null)
                {
                    Artwork fanArt = metadata.Artwork.FirstOrDefault(a => a.ArtworkKind == ArtworkKind.FanArt);
                    if (fanArt == null)
                    {
                        fanArt = new Artwork { ArtworkKind = ArtworkKind.FanArt };
                        metadata.Artwork.Add(fanArt);
                    }

                    fanArt.Path = incomingFanArt.Path;
                    fanArt.DateAdded = incomingFanArt.DateAdded;
                    fanArt.DateUpdated = incomingFanArt.DateUpdated;
                }
            }

            await dbContext.SaveChangesAsync();

            return Unit.Default;
        }

        public async Task<bool> AddEpisode(JellyfinEpisode episode)
        {
            await using TvContext dbContext = _dbContextFactory.CreateDbContext();
            await dbContext.AddAsync(episode);
            if (await dbContext.SaveChangesAsync() <= 0)
            {
                return false;
            }

            await dbContext.Entry(episode).Reference(m => m.LibraryPath).LoadAsync();
            await dbContext.Entry(episode.LibraryPath).Reference(lp => lp.Library).LoadAsync();
            return true;
        }

        public async Task<Unit> Update(JellyfinEpisode episode)
        {
            await using TvContext dbContext = _dbContextFactory.CreateDbContext();
            Option<JellyfinEpisode> maybeExisting = await dbContext.JellyfinEpisodes
                .Include(m => m.LibraryPath)
                .Include(m => m.MediaVersions)
                .ThenInclude(mv => mv.MediaFiles)
                .Include(m => m.MediaVersions)
                .ThenInclude(mv => mv.Streams)
                .Include(m => m.EpisodeMetadata)
                .ThenInclude(mm => mm.Artwork)
                .Filter(m => m.ItemId == episode.ItemId)
                .OrderBy(m => m.ItemId)
                .SingleOrDefaultAsync();

            if (maybeExisting.IsSome)
            {
                JellyfinEpisode existing = maybeExisting.ValueUnsafe();

                // library path is used for search indexing later
                episode.LibraryPath = existing.LibraryPath;

                existing.Etag = episode.Etag;
                existing.EpisodeNumber = episode.EpisodeNumber;

                // metadata
                EpisodeMetadata metadata = existing.EpisodeMetadata.Head();
                EpisodeMetadata incomingMetadata = episode.EpisodeMetadata.Head();
                metadata.Title = incomingMetadata.Title;
                metadata.SortTitle = incomingMetadata.SortTitle;
                metadata.Plot = incomingMetadata.Plot;
                metadata.Year = incomingMetadata.Year;
                metadata.DateAdded = incomingMetadata.DateAdded;
                metadata.DateUpdated = DateTime.UtcNow;
                metadata.ReleaseDate = incomingMetadata.ReleaseDate;

                // thumbnail
                Artwork incomingThumbnail =
                    incomingMetadata.Artwork.FirstOrDefault(a => a.ArtworkKind == ArtworkKind.Thumbnail);
                if (incomingThumbnail != null)
                {
                    Artwork thumbnail = metadata.Artwork.FirstOrDefault(a => a.ArtworkKind == ArtworkKind.Thumbnail);
                    if (thumbnail == null)
                    {
                        thumbnail = new Artwork { ArtworkKind = ArtworkKind.Thumbnail };
                        metadata.Artwork.Add(thumbnail);
                    }

                    thumbnail.Path = incomingThumbnail.Path;
                    thumbnail.DateAdded = incomingThumbnail.DateAdded;
                    thumbnail.DateUpdated = incomingThumbnail.DateUpdated;
                }

                // version
                MediaVersion version = existing.MediaVersions.Head();
                MediaVersion incomingVersion = episode.MediaVersions.Head();
                version.Name = incomingVersion.Name;
                version.DateAdded = incomingVersion.DateAdded;

                // media file
                MediaFile file = version.MediaFiles.Head();
                MediaFile incomingFile = incomingVersion.MediaFiles.Head();
                file.Path = incomingFile.Path;
            }

            await dbContext.SaveChangesAsync();

            return Unit.Default;
        }
    }
}
