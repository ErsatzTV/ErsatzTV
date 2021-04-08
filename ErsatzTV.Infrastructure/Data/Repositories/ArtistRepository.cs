﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Metadata;
using LanguageExt;
using Microsoft.EntityFrameworkCore;
using static LanguageExt.Prelude;

namespace ErsatzTV.Infrastructure.Data.Repositories
{
    public class ArtistRepository : IArtistRepository
    {
        private readonly IDbConnection _dbConnection;
        private readonly IDbContextFactory<TvContext> _dbContextFactory;

        public ArtistRepository(IDbContextFactory<TvContext> dbContextFactory, IDbConnection dbConnection)
        {
            _dbContextFactory = dbContextFactory;
            _dbConnection = dbConnection;
        }

        public async Task<Option<Artist>> GetArtistByMetadata(int libraryPathId, ArtistMetadata metadata)
        {
            await using TvContext dbContext = _dbContextFactory.CreateDbContext();
            Option<int> maybeId = await dbContext.ArtistMetadata
                .Where(
                    s => s.Title == metadata.Title && (metadata.MetadataKind == MetadataKind.Fallback ||
                                                       s.Disambiguation == metadata.Disambiguation))
                .Where(s => s.Artist.LibraryPathId == libraryPathId)
                .SingleOrDefaultAsync()
                .Map(Optional)
                .MapT(sm => sm.ArtistId);

            return await maybeId.Match(
                id =>
                {
                    return dbContext.Artists
                        .AsNoTracking()
                        .Include(s => s.ArtistMetadata)
                        .ThenInclude(sm => sm.Artwork)
                        .Include(s => s.ArtistMetadata)
                        .ThenInclude(sm => sm.Genres)
                        .Include(s => s.ArtistMetadata)
                        .ThenInclude(sm => sm.Styles)
                        .Include(s => s.ArtistMetadata)
                        .ThenInclude(sm => sm.Moods)
                        .Include(s => s.LibraryPath)
                        .ThenInclude(lp => lp.Library)
                        .OrderBy(s => s.Id)
                        .SingleOrDefaultAsync(s => s.Id == id)
                        .Map(Optional);
                },
                () => Option<Artist>.None.AsTask());
        }

        public async Task<Either<BaseError, MediaItemScanResult<Artist>>> AddArtist(
            int libraryPathId,
            string artistFolder,
            ArtistMetadata metadata)
        {
            await using TvContext dbContext = _dbContextFactory.CreateDbContext();

            try
            {
                metadata.DateAdded = DateTime.UtcNow;
                metadata.Genres ??= new List<Genre>();
                metadata.Styles ??= new List<Style>();
                metadata.Moods ??= new List<Mood>();
                var artist = new Artist
                {
                    LibraryPathId = libraryPathId,
                    ArtistMetadata = new List<ArtistMetadata> { metadata }
                };

                await dbContext.Artists.AddAsync(artist);
                await dbContext.SaveChangesAsync();
                await dbContext.Entry(artist).Reference(s => s.LibraryPath).LoadAsync();
                await dbContext.Entry(artist.LibraryPath).Reference(lp => lp.Library).LoadAsync();

                return new MediaItemScanResult<Artist>(artist) { IsAdded = true };
            }
            catch (Exception ex)
            {
                return BaseError.New(ex.Message);
            }
        }

        public async Task<List<int>> DeleteEmptyArtists(LibraryPath libraryPath)
        {
            await using TvContext dbContext = _dbContextFactory.CreateDbContext();
            List<Artist> artists = await dbContext.Artists
                .Filter(a => a.LibraryPathId == libraryPath.Id)
                .Filter(a => a.MusicVideos.Count == 0)
                .ToListAsync();
            var ids = artists.Map(a => a.Id).ToList();
            dbContext.Artists.RemoveRange(artists);
            await dbContext.SaveChangesAsync();
            return ids;
        }

        public async Task<List<ArtistMetadata>> GetArtistsForCards(List<int> ids)
        {
            await using TvContext dbContext = _dbContextFactory.CreateDbContext();
            return await dbContext.ArtistMetadata
                .AsNoTracking()
                .Filter(am => ids.Contains(am.ArtistId))
                .Include(am => am.Artwork)
                .OrderBy(am => am.SortTitle)
                .ToListAsync();
        }

        public Task<bool> AddGenre(ArtistMetadata metadata, Genre genre) =>
            _dbConnection.ExecuteAsync(
                "INSERT INTO Genre (Name, ArtistMetadataId) VALUES (@Name, @MetadataId)",
                new { genre.Name, MetadataId = metadata.Id }).Map(result => result > 0);

        public Task<bool> AddStyle(ArtistMetadata metadata, Style style) =>
            _dbConnection.ExecuteAsync(
                "INSERT INTO Style (Name, ArtistMetadataId) VALUES (@Name, @MetadataId)",
                new { style.Name, MetadataId = metadata.Id }).Map(result => result > 0);

        public Task<bool> AddMood(ArtistMetadata metadata, Mood mood) =>
            _dbConnection.ExecuteAsync(
                "INSERT INTO Mood (Name, ArtistMetadataId) VALUES (@Name, @MetadataId)",
                new { mood.Name, MetadataId = metadata.Id }).Map(result => result > 0);
    }
}
