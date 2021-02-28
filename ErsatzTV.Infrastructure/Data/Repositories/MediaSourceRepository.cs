using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using Microsoft.EntityFrameworkCore;
using static LanguageExt.Prelude;

namespace ErsatzTV.Infrastructure.Data.Repositories
{
    public class MediaSourceRepository : IMediaSourceRepository
    {
        private readonly IDbConnection _dbConnection;
        private readonly IDbContextFactory<TvContext> _dbContextFactory;

        public MediaSourceRepository(
            IDbContextFactory<TvContext> dbContextFactory,
            IDbConnection dbConnection)
        {
            _dbContextFactory = dbContextFactory;
            _dbConnection = dbConnection;
        }

        public async Task<LocalMediaSource> Add(LocalMediaSource localMediaSource)
        {
            await using TvContext context = _dbContextFactory.CreateDbContext();
            await context.LocalMediaSources.AddAsync(localMediaSource);
            await context.SaveChangesAsync();
            return localMediaSource;
        }

        public async Task<PlexMediaSource> Add(PlexMediaSource plexMediaSource)
        {
            await using TvContext context = _dbContextFactory.CreateDbContext();
            await context.PlexMediaSources.AddAsync(plexMediaSource);
            await context.SaveChangesAsync();
            return plexMediaSource;
        }

        public async Task<List<MediaSource>> GetAll()
        {
            await using TvContext context = _dbContextFactory.CreateDbContext();
            List<MediaSource> all = await context.MediaSources.ToListAsync();
            foreach (PlexMediaSource plex in all.OfType<PlexMediaSource>())
            {
                await context.Entry(plex).Collection(p => p.Connections).LoadAsync();
            }

            return all;
        }

        public Task<List<PlexMediaSource>> GetAllPlex()
        {
            using TvContext context = _dbContextFactory.CreateDbContext();
            return context.PlexMediaSources
                .Include(p => p.Connections)
                .ToListAsync();
        }

        public Task<List<PlexLibrary>> GetPlexLibraries(int plexMediaSourceId)
        {
            using TvContext context = _dbContextFactory.CreateDbContext();
            return context.PlexLibraries
                .Filter(l => l.MediaSourceId == plexMediaSourceId)
                .ToListAsync();
        }

        public Task<Option<PlexLibrary>> GetPlexLibrary(int plexLibraryId)
        {
            using TvContext context = _dbContextFactory.CreateDbContext();
            return context.PlexLibraries
                .OrderBy(l => l.Id) // https://github.com/dotnet/efcore/issues/22579
                .SingleOrDefaultAsync(l => l.Id == plexLibraryId)
                .Map(Optional);
        }

        public Task<Option<MediaSource>> Get(int id)
        {
            using TvContext context = _dbContextFactory.CreateDbContext();
            return context.MediaSources
                .OrderBy(s => s.Id) // https://github.com/dotnet/efcore/issues/22579
                .SingleOrDefaultAsync(s => s.Id == id)
                .Map(Optional);
        }

        public Task<Option<PlexMediaSource>> GetPlex(int id)
        {
            using TvContext context = _dbContextFactory.CreateDbContext();
            return context.PlexMediaSources
                .Include(p => p.Connections)
                .Include(p => p.Libraries)
                .OrderBy(s => s.Id) // https://github.com/dotnet/efcore/issues/22579
                .SingleOrDefaultAsync(p => p.Id == id)
                .Map(Optional);
        }

        public Task<int> CountMediaItems(int id)
        {
            using TvContext context = _dbContextFactory.CreateDbContext();
            return context.MediaItems
                .CountAsync(i => i.LibraryPath.Library.MediaSourceId == id);
        }

        public async Task Update(LocalMediaSource localMediaSource)
        {
            await using TvContext context = _dbContextFactory.CreateDbContext();
            context.LocalMediaSources.Update(localMediaSource);
            await context.SaveChangesAsync();
        }

        public async Task Update(PlexMediaSource plexMediaSource)
        {
            await using TvContext context = _dbContextFactory.CreateDbContext();
            context.PlexMediaSources.Update(plexMediaSource);
            await context.SaveChangesAsync();
        }

        public async Task Update(PlexLibrary plexMediaSourceLibrary)
        {
            await using TvContext context = _dbContextFactory.CreateDbContext();
            context.PlexLibraries.Update(plexMediaSourceLibrary);
            await context.SaveChangesAsync();
        }

        public async Task Delete(int id)
        {
            await using TvContext context = _dbContextFactory.CreateDbContext();
            MediaSource mediaSource = await context.MediaSources.FindAsync(id);
            context.MediaSources.Remove(mediaSource);
            await context.SaveChangesAsync();
        }

        public Task DisablePlexLibrarySync(IEnumerable<int> libraryIds) =>
            _dbConnection.ExecuteAsync(
                "UPDATE PlexLibrary SET ShouldSyncItems = 0 WHERE Id in @ids",
                new { ids = libraryIds });

        public Task EnablePlexLibrarySync(IEnumerable<int> libraryIds) =>
            _dbConnection.ExecuteAsync(
                "UPDATE PlexLibrary SET ShouldSyncItems = 1 WHERE Id in @ids",
                new { ids = libraryIds });
    }
}
