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

        public async Task<PlexMediaSource> Add(PlexMediaSource plexMediaSource)
        {
            await using TvContext context = _dbContextFactory.CreateDbContext();
            await context.PlexMediaSources.AddAsync(plexMediaSource);
            await context.SaveChangesAsync();
            return plexMediaSource;
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

        public Task<List<PlexPathReplacement>> GetPlexPathReplacements(int plexMediaSourceId)
        {
            using TvContext context = _dbContextFactory.CreateDbContext();
            return context.PlexPathReplacements
                .Include(ppr => ppr.PlexMediaSource)
                .Filter(r => r.PlexMediaSourceId == plexMediaSourceId)
                .ToListAsync();
        }

        public Task<Option<PlexLibrary>> GetPlexLibrary(int plexLibraryId)
        {
            using TvContext context = _dbContextFactory.CreateDbContext();
            return context.PlexLibraries
                .Include(l => l.Paths)
                .OrderBy(l => l.Id) // https://github.com/dotnet/efcore/issues/22579
                .SingleOrDefaultAsync(l => l.Id == plexLibraryId)
                .Map(Optional);
        }

        public Task<Option<PlexMediaSource>> GetPlex(int id)
        {
            using TvContext context = _dbContextFactory.CreateDbContext();
            return context.PlexMediaSources
                .Include(p => p.Connections)
                .Include(p => p.Libraries)
                .Include(p => p.PathReplacements)
                .OrderBy(s => s.Id) // https://github.com/dotnet/efcore/issues/22579
                .SingleOrDefaultAsync(p => p.Id == id)
                .Map(Optional);
        }

        public async Task<Option<PlexMediaSource>> GetPlexByLibraryId(int plexLibraryId)
        {
            int? id = await _dbConnection.QuerySingleAsync<int?>(
                @"SELECT L.MediaSourceId FROM Library L
                INNER JOIN PlexLibrary PL on L.Id = PL.Id
                WHERE L.Id = @PlexLibraryId",
                new { PlexLibraryId = plexLibraryId });

            await using TvContext context = _dbContextFactory.CreateDbContext();
            return await context.PlexMediaSources
                .Include(p => p.Connections)
                .Include(p => p.Libraries)
                .OrderBy(p => p.Id)
                .SingleOrDefaultAsync(p => p.Id == id)
                .Map(Optional);
        }

        public Task<List<PlexPathReplacement>> GetPlexPathReplacementsByLibraryId(int plexLibraryPathId)
        {
            using TvContext context = _dbContextFactory.CreateDbContext();
            return context.PlexPathReplacements
                .FromSqlRaw(
                    @"select ppr.* from LibraryPath lp
                    inner join PlexLibrary pl ON pl.Id = lp.LibraryId
                    inner join Library l ON l.Id = pl.Id
                    inner join PlexPathReplacement ppr on ppr.PlexMediaSourceId = l.MediaSourceId
                    where lp.Id = {0}",
                    plexLibraryPathId)
                .Include(ppr => ppr.PlexMediaSource)
                .ToListAsync();
        }

        public async Task Update(
            PlexMediaSource plexMediaSource,
            List<PlexConnection> sortedConnections,
            List<PlexConnection> toAdd,
            List<PlexConnection> toDelete)
        {
            await _dbConnection.ExecuteAsync(
                @"UPDATE PlexMediaSource SET
                  ProductVersion = @ProductVersion,
                  Platform = @Platform,
                  PlatformVersion = @PlatformVersion,
                  ServerName = @ServerName
                  WHERE Id = @Id",
                new
                {
                    plexMediaSource.ProductVersion,
                    plexMediaSource.Platform,
                    plexMediaSource.PlatformVersion,
                    plexMediaSource.ServerName,
                    plexMediaSource.Id
                });

            foreach (PlexConnection add in toAdd)
            {
                await _dbConnection.ExecuteAsync(
                    @"INSERT INTO PlexConnection (IsActive, Uri, PlexMediaSourceId)
                    VALUES (0, @Uri, @PlexMediaSourceId)",
                    new { add.Uri, PlexMediaSourceId = plexMediaSource.Id });
            }

            foreach (PlexConnection delete in toDelete)
            {
                await _dbConnection.ExecuteAsync(
                    @"DELETE FROM PlexConnection WHERE Id = @Id",
                    new { delete.Id });
            }

            int activeCount = await _dbConnection.QuerySingleAsync<int>(
                @"SELECT COUNT(*) FROM PlexConnection WHERE IsActive = 1 AND PlexMediaSourceId = @PlexMediaSourceId",
                new { PlexMediaSourceId = plexMediaSource.Id });
            if (activeCount == 0)
            {
                Option<PlexConnection> toActivate =
                    sortedConnections.FirstOrDefault(c => toDelete.All(d => d.Id != c.Id));

                // update on uri because connections from Plex API don't have our local ids
                await toActivate.IfSomeAsync(
                    async c => await _dbConnection.ExecuteAsync(
                        @"UPDATE PlexConnection SET IsActive = 1 WHERE Uri = @Uri",
                        new { c.Uri }));
            }
        }

        public async Task<List<int>> UpdateLibraries(
            int plexMediaSourceId,
            List<PlexLibrary> toAdd,
            List<PlexLibrary> toDelete)
        {
            await using TvContext dbContext = _dbContextFactory.CreateDbContext();

            foreach (PlexLibrary add in toAdd)
            {
                add.MediaSourceId = plexMediaSourceId;
                dbContext.Entry(add).State = EntityState.Added;
                foreach (LibraryPath path in add.Paths)
                {
                    dbContext.Entry(path).State = EntityState.Added;
                }
            }

            foreach (PlexLibrary delete in toDelete)
            {
                dbContext.Entry(delete).State = EntityState.Deleted;
            }

            List<int> ids = await DisablePlexLibrarySync(toDelete.Map(l => l.Id).ToList());

            await dbContext.SaveChangesAsync();

            return ids;
        }

        public async Task<List<int>> UpdateLibraries(
            int jellyfinMediaSourceId,
            List<JellyfinLibrary> toAdd,
            List<JellyfinLibrary> toDelete)
        {
            await using TvContext dbContext = _dbContextFactory.CreateDbContext();

            foreach (JellyfinLibrary add in toAdd)
            {
                add.MediaSourceId = jellyfinMediaSourceId;
                dbContext.Entry(add).State = EntityState.Added;
                foreach (LibraryPath path in add.Paths)
                {
                    dbContext.Entry(path).State = EntityState.Added;
                }
            }

            foreach (JellyfinLibrary delete in toDelete)
            {
                dbContext.Entry(delete).State = EntityState.Deleted;
            }

            List<int> ids = await DisableJellyfinLibrarySync(toDelete.Map(l => l.Id).ToList());

            await dbContext.SaveChangesAsync();

            return ids;
        }

        public async Task<List<int>> UpdateLibraries(
            int embyMediaSourceId,
            List<EmbyLibrary> toAdd,
            List<EmbyLibrary> toDelete)
        {
            await using TvContext dbContext = _dbContextFactory.CreateDbContext();

            foreach (EmbyLibrary add in toAdd)
            {
                add.MediaSourceId = embyMediaSourceId;
                dbContext.Entry(add).State = EntityState.Added;
                foreach (LibraryPath path in add.Paths)
                {
                    dbContext.Entry(path).State = EntityState.Added;
                }
            }

            foreach (EmbyLibrary delete in toDelete)
            {
                dbContext.Entry(delete).State = EntityState.Deleted;
            }

            List<int> ids = await DisableEmbyLibrarySync(toDelete.Map(l => l.Id).ToList());

            await dbContext.SaveChangesAsync();

            return ids;
        }

        public async Task<Unit> UpdatePathReplacements(
            int plexMediaSourceId,
            List<PlexPathReplacement> toAdd,
            List<PlexPathReplacement> toUpdate,
            List<PlexPathReplacement> toDelete)
        {
            foreach (PlexPathReplacement add in toAdd)
            {
                await _dbConnection.ExecuteAsync(
                    @"INSERT INTO PlexPathReplacement
                    (PlexPath, LocalPath, PlexMediaSourceId)
                    VALUES (@PlexPath, @LocalPath, @PlexMediaSourceId)",
                    new { add.PlexPath, add.LocalPath, PlexMediaSourceId = plexMediaSourceId });
            }

            foreach (PlexPathReplacement update in toUpdate)
            {
                await _dbConnection.ExecuteAsync(
                    @"UPDATE PlexPathReplacement
                    SET PlexPath = @PlexPath, LocalPath = @LocalPath
                    WHERE Id = @Id",
                    new { update.PlexPath, update.LocalPath, update.Id });
            }

            foreach (PlexPathReplacement delete in toDelete)
            {
                await _dbConnection.ExecuteAsync(
                    @"DELETE FROM PlexPathReplacement WHERE Id = @Id",
                    new { delete.Id });
            }

            return Unit.Default;
        }

        public async Task<List<int>> DeleteAllPlex()
        {
            await using TvContext context = _dbContextFactory.CreateDbContext();

            List<PlexMediaSource> allMediaSources = await context.PlexMediaSources.ToListAsync();
            context.PlexMediaSources.RemoveRange(allMediaSources);

            List<PlexLibrary> allPlexLibraries = await context.PlexLibraries.ToListAsync();
            context.PlexLibraries.RemoveRange(allPlexLibraries);

            List<int> movieIds = await context.PlexMovies.Map(pm => pm.Id).ToListAsync();
            List<int> showIds = await context.PlexShows.Map(ps => ps.Id).ToListAsync();
            List<int> episodeIds = await context.PlexEpisodes.Map(pe => pe.Id).ToListAsync();

            await context.SaveChangesAsync();

            return movieIds.Append(showIds).Append(episodeIds).ToList();
        }

        public async Task<List<int>> DeletePlex(PlexMediaSource plexMediaSource)
        {
            List<int> mediaItemIds = await _dbConnection.QueryAsync<int>(
                    @"SELECT MediaItem.Id FROM MediaItem
                  INNER JOIN LibraryPath LP on MediaItem.LibraryPathId = LP.Id
                  INNER JOIN Library L on LP.LibraryId = L.Id
                  WHERE L.MediaSourceId = @PlexMediaSourceId",
                    new { PlexMediaSourceId = plexMediaSource.Id })
                .Map(result => result.ToList());

            await _dbConnection.ExecuteAsync(
                @"DELETE FROM MediaSource WHERE Id = @PlexMediaSourceId",
                new { PlexMediaSourceId = plexMediaSource.Id });

            return mediaItemIds;
        }

        public async Task<List<int>> DisablePlexLibrarySync(List<int> libraryIds)
        {
            await _dbConnection.ExecuteAsync(
                "UPDATE PlexLibrary SET ShouldSyncItems = 0 WHERE Id IN @ids",
                new { ids = libraryIds });

            await _dbConnection.ExecuteAsync(
                "UPDATE Library SET LastScan = null WHERE Id IN @ids",
                new { ids = libraryIds });

            List<int> movieIds = await _dbConnection.QueryAsync<int>(
                @"SELECT m.Id FROM MediaItem m
                INNER JOIN PlexMovie pm ON pm.Id = m.Id
                INNER JOIN LibraryPath lp ON lp.Id = m.LibraryPathId
                INNER JOIN Library l ON l.Id = lp.LibraryId
                WHERE l.Id IN @ids",
                new { ids = libraryIds }).Map(result => result.ToList());

            await _dbConnection.ExecuteAsync(
                @"DELETE FROM MediaItem WHERE Id IN
                (SELECT m.Id FROM MediaItem m
                INNER JOIN PlexMovie pm ON pm.Id = m.Id
                INNER JOIN LibraryPath lp ON lp.Id = m.LibraryPathId
                INNER JOIN Library l ON l.Id = lp.LibraryId
                WHERE l.Id IN @ids)",
                new { ids = libraryIds });

            List<int> episodeIds = await _dbConnection.QueryAsync<int>(
                @"SELECT m.Id FROM MediaItem m
                INNER JOIN PlexEpisode pe ON pe.Id = m.Id
                INNER JOIN LibraryPath lp ON lp.Id = m.LibraryPathId
                INNER JOIN Library l ON l.Id = lp.LibraryId
                WHERE l.Id IN @ids",
                new { ids = libraryIds }).Map(result => result.ToList());

            await _dbConnection.ExecuteAsync(
                @"DELETE FROM MediaItem WHERE Id IN
                (SELECT m.Id FROM MediaItem m
                INNER JOIN PlexEpisode pe ON pe.Id = m.Id
                INNER JOIN LibraryPath lp ON lp.Id = m.LibraryPathId
                INNER JOIN Library l ON l.Id = lp.LibraryId
                WHERE l.Id IN @ids)",
                new { ids = libraryIds });

            List<int> seasonIds = await _dbConnection.QueryAsync<int>(
                @"SELECT m.Id FROM MediaItem m
                INNER JOIN PlexSeason ps ON ps.Id = m.Id
                INNER JOIN LibraryPath lp ON lp.Id = m.LibraryPathId
                INNER JOIN Library l ON l.Id = lp.LibraryId
                WHERE l.Id IN @ids",
                new { ids = libraryIds }).Map(result => result.ToList());

            await _dbConnection.ExecuteAsync(
                @"DELETE FROM MediaItem WHERE Id IN
                (SELECT m.Id FROM MediaItem m
                INNER JOIN PlexSeason ps ON ps.Id = m.Id
                INNER JOIN LibraryPath lp ON lp.Id = m.LibraryPathId
                INNER JOIN Library l ON l.Id = lp.LibraryId
                WHERE l.Id IN @ids)",
                new { ids = libraryIds });

            List<int> showIds = await _dbConnection.QueryAsync<int>(
                @"SELECT m.Id FROM MediaItem m
                INNER JOIN PlexShow ps ON ps.Id = m.Id
                INNER JOIN LibraryPath lp ON lp.Id = m.LibraryPathId
                INNER JOIN Library l ON l.Id = lp.LibraryId
                WHERE l.Id IN @ids",
                new { ids = libraryIds }).Map(result => result.ToList());

            await _dbConnection.ExecuteAsync(
                @"DELETE FROM MediaItem WHERE Id IN
                (SELECT m.Id FROM MediaItem m
                INNER JOIN PlexShow ps ON ps.Id = m.Id
                INNER JOIN LibraryPath lp ON lp.Id = m.LibraryPathId
                INNER JOIN Library l ON l.Id = lp.LibraryId
                WHERE l.Id IN @ids)",
                new { ids = libraryIds });

            return movieIds.Append(showIds).Append(seasonIds).Append(episodeIds).ToList();
        }

        public Task EnablePlexLibrarySync(IEnumerable<int> libraryIds) =>
            _dbConnection.ExecuteAsync(
                "UPDATE PlexLibrary SET ShouldSyncItems = 1 WHERE Id IN @ids",
                new { ids = libraryIds });

        public async Task<Unit> UpsertJellyfin(string address, string serverName, string operatingSystem)
        {
            await using TvContext dbContext = _dbContextFactory.CreateDbContext();
            Option<JellyfinMediaSource> maybeExisting = dbContext.JellyfinMediaSources
                .Include(ms => ms.Connections)
                .OrderBy(ms => ms.Id)
                .HeadOrNone();

            return await maybeExisting.Match(
                async jellyfinMediaSource =>
                {
                    if (!jellyfinMediaSource.Connections.Any())
                    {
                        jellyfinMediaSource.Connections.Add(new JellyfinConnection { Address = address });
                    }
                    else if (jellyfinMediaSource.Connections.Head().Address != address)
                    {
                        jellyfinMediaSource.Connections.Head().Address = address;
                    }

                    if (jellyfinMediaSource.ServerName != serverName)
                    {
                        jellyfinMediaSource.ServerName = serverName;
                    }

                    if (jellyfinMediaSource.OperatingSystem != operatingSystem)
                    {
                        jellyfinMediaSource.OperatingSystem = operatingSystem;
                    }

                    await dbContext.SaveChangesAsync();

                    return Unit.Default;
                },
                async () =>
                {
                    var mediaSource = new JellyfinMediaSource
                    {
                        ServerName = serverName,
                        OperatingSystem = operatingSystem,
                        Connections = new List<JellyfinConnection>
                        {
                            new() { Address = address }
                        },
                        PathReplacements = new List<JellyfinPathReplacement>()
                    };

                    await dbContext.AddAsync(mediaSource);
                    await dbContext.SaveChangesAsync();

                    return Unit.Default;
                });
        }

        public Task<List<JellyfinMediaSource>> GetAllJellyfin()
        {
            using TvContext context = _dbContextFactory.CreateDbContext();
            return context.JellyfinMediaSources
                .Include(p => p.Connections)
                .ToListAsync();
        }

        public Task<Option<JellyfinMediaSource>> GetJellyfin(int id)
        {
            using TvContext context = _dbContextFactory.CreateDbContext();
            return context.JellyfinMediaSources
                .Include(p => p.Connections)
                .Include(p => p.Libraries)
                .Include(p => p.PathReplacements)
                .OrderBy(s => s.Id) // https://github.com/dotnet/efcore/issues/22579
                .SingleOrDefaultAsync(p => p.Id == id)
                .Map(Optional);
        }

        public Task<List<JellyfinLibrary>> GetJellyfinLibraries(int jellyfinMediaSourceId)
        {
            using TvContext context = _dbContextFactory.CreateDbContext();
            return context.JellyfinLibraries
                .Filter(l => l.MediaSourceId == jellyfinMediaSourceId)
                .ToListAsync();
        }

        public Task<Unit> EnableJellyfinLibrarySync(IEnumerable<int> libraryIds) =>
            _dbConnection.ExecuteAsync(
                "UPDATE JellyfinLibrary SET ShouldSyncItems = 1 WHERE Id IN @ids",
                new { ids = libraryIds }).Map(_ => Unit.Default);

        public async Task<List<int>> DisableJellyfinLibrarySync(List<int> libraryIds)
        {
            await _dbConnection.ExecuteAsync(
                "UPDATE JellyfinLibrary SET ShouldSyncItems = 0 WHERE Id IN @ids",
                new { ids = libraryIds });

            await _dbConnection.ExecuteAsync(
                "UPDATE Library SET LastScan = null WHERE Id IN @ids",
                new { ids = libraryIds });

            List<int> movieIds = await _dbConnection.QueryAsync<int>(
                @"SELECT m.Id FROM MediaItem m
                INNER JOIN JellyfinMovie pm ON pm.Id = m.Id
                INNER JOIN LibraryPath lp ON lp.Id = m.LibraryPathId
                INNER JOIN Library l ON l.Id = lp.LibraryId
                WHERE l.Id IN @ids",
                new { ids = libraryIds }).Map(result => result.ToList());

            await _dbConnection.ExecuteAsync(
                @"DELETE FROM MediaItem WHERE Id IN
                (SELECT m.Id FROM MediaItem m
                INNER JOIN JellyfinMovie pm ON pm.Id = m.Id
                INNER JOIN LibraryPath lp ON lp.Id = m.LibraryPathId
                INNER JOIN Library l ON l.Id = lp.LibraryId
                WHERE l.Id IN @ids)",
                new { ids = libraryIds });

            List<int> episodeIds = await _dbConnection.QueryAsync<int>(
                @"SELECT m.Id FROM MediaItem m
                INNER JOIN JellyfinEpisode pe ON pe.Id = m.Id
                INNER JOIN LibraryPath lp ON lp.Id = m.LibraryPathId
                INNER JOIN Library l ON l.Id = lp.LibraryId
                WHERE l.Id IN @ids",
                new { ids = libraryIds }).Map(result => result.ToList());

            await _dbConnection.ExecuteAsync(
                @"DELETE FROM MediaItem WHERE Id IN
                (SELECT m.Id FROM MediaItem m
                INNER JOIN JellyfinEpisode pe ON pe.Id = m.Id
                INNER JOIN LibraryPath lp ON lp.Id = m.LibraryPathId
                INNER JOIN Library l ON l.Id = lp.LibraryId
                WHERE l.Id IN @ids)",
                new { ids = libraryIds });

            List<int> seasonIds = await _dbConnection.QueryAsync<int>(
                @"SELECT m.Id FROM MediaItem m
                INNER JOIN JellyfinSeason js ON js.Id = m.Id
                INNER JOIN LibraryPath lp ON lp.Id = m.LibraryPathId
                INNER JOIN Library l ON l.Id = lp.LibraryId
                WHERE l.Id IN @ids",
                new { ids = libraryIds }).Map(result => result.ToList());

            await _dbConnection.ExecuteAsync(
                @"DELETE FROM MediaItem WHERE Id IN
                (SELECT m.Id FROM MediaItem m
                INNER JOIN JellyfinSeason ps ON ps.Id = m.Id
                INNER JOIN LibraryPath lp ON lp.Id = m.LibraryPathId
                INNER JOIN Library l ON l.Id = lp.LibraryId
                WHERE l.Id IN @ids)",
                new { ids = libraryIds });

            List<int> showIds = await _dbConnection.QueryAsync<int>(
                @"SELECT m.Id FROM MediaItem m
                INNER JOIN JellyfinShow ps ON ps.Id = m.Id
                INNER JOIN LibraryPath lp ON lp.Id = m.LibraryPathId
                INNER JOIN Library l ON l.Id = lp.LibraryId
                WHERE l.Id IN @ids",
                new { ids = libraryIds }).Map(result => result.ToList());

            await _dbConnection.ExecuteAsync(
                @"DELETE FROM MediaItem WHERE Id IN
                (SELECT m.Id FROM MediaItem m
                INNER JOIN JellyfinShow ps ON ps.Id = m.Id
                INNER JOIN LibraryPath lp ON lp.Id = m.LibraryPathId
                INNER JOIN Library l ON l.Id = lp.LibraryId
                WHERE l.Id IN @ids)",
                new { ids = libraryIds });

            return movieIds.Append(showIds).Append(seasonIds).Append(episodeIds).ToList();
        }

        public Task<Option<JellyfinLibrary>> GetJellyfinLibrary(int jellyfinLibraryId)
        {
            using TvContext context = _dbContextFactory.CreateDbContext();
            return context.JellyfinLibraries
                .Include(l => l.Paths)
                .OrderBy(l => l.Id) // https://github.com/dotnet/efcore/issues/22579
                .SingleOrDefaultAsync(l => l.Id == jellyfinLibraryId)
                .Map(Optional);
        }

        public async Task<Option<JellyfinMediaSource>> GetJellyfinByLibraryId(int jellyfinLibraryId)
        {
            int? id = await _dbConnection.QuerySingleAsync<int?>(
                @"SELECT L.MediaSourceId FROM Library L
                INNER JOIN JellyfinLibrary PL on L.Id = PL.Id
                WHERE L.Id = @JellyfinLibraryId",
                new { JellyfinLibraryId = jellyfinLibraryId });

            await using TvContext context = _dbContextFactory.CreateDbContext();
            return await context.JellyfinMediaSources
                .Include(p => p.Connections)
                .Include(p => p.Libraries)
                .OrderBy(p => p.Id)
                .SingleOrDefaultAsync(p => p.Id == id)
                .Map(Optional);
        }

        public Task<List<JellyfinPathReplacement>> GetJellyfinPathReplacements(int jellyfinMediaSourceId)
        {
            using TvContext context = _dbContextFactory.CreateDbContext();
            return context.JellyfinPathReplacements
                .Filter(r => r.JellyfinMediaSourceId == jellyfinMediaSourceId)
                .Include(jpr => jpr.JellyfinMediaSource)
                .ToListAsync();
        }

        public Task<List<JellyfinPathReplacement>> GetJellyfinPathReplacementsByLibraryId(int jellyfinLibraryPathId)
        {
            using TvContext context = _dbContextFactory.CreateDbContext();
            return context.JellyfinPathReplacements
                .FromSqlRaw(
                    @"select jpr.* from LibraryPath lp
                    inner join JellyfinLibrary jl ON jl.Id = lp.LibraryId
                    inner join Library l ON l.Id = jl.Id
                    inner join JellyfinPathReplacement jpr on jpr.JellyfinMediaSourceId = l.MediaSourceId
                    where lp.Id = {0}",
                    jellyfinLibraryPathId)
                .Include(jpr => jpr.JellyfinMediaSource)
                .ToListAsync();
        }

        public async Task<Unit> UpdatePathReplacements(
            int jellyfinMediaSourceId,
            List<JellyfinPathReplacement> toAdd,
            List<JellyfinPathReplacement> toUpdate,
            List<JellyfinPathReplacement> toDelete)
        {
            foreach (JellyfinPathReplacement add in toAdd)
            {
                await _dbConnection.ExecuteAsync(
                    @"INSERT INTO JellyfinPathReplacement
                    (JellyfinPath, LocalPath, JellyfinMediaSourceId)
                    VALUES (@JellyfinPath, @LocalPath, @JellyfinMediaSourceId)",
                    new { add.JellyfinPath, add.LocalPath, JellyfinMediaSourceId = jellyfinMediaSourceId });
            }

            foreach (JellyfinPathReplacement update in toUpdate)
            {
                await _dbConnection.ExecuteAsync(
                    @"UPDATE JellyfinPathReplacement
                    SET JellyfinPath = @JellyfinPath, LocalPath = @LocalPath
                    WHERE Id = @Id",
                    new { update.JellyfinPath, update.LocalPath, update.Id });
            }

            foreach (JellyfinPathReplacement delete in toDelete)
            {
                await _dbConnection.ExecuteAsync(
                    @"DELETE FROM JellyfinPathReplacement WHERE Id = @Id",
                    new { delete.Id });
            }

            return Unit.Default;
        }

        public async Task<List<int>> DeleteAllJellyfin()
        {
            await using TvContext context = _dbContextFactory.CreateDbContext();

            List<JellyfinMediaSource> allMediaSources = await context.JellyfinMediaSources.ToListAsync();
            var mediaSourceIds = allMediaSources.Map(ms => ms.Id).ToList();
            context.JellyfinMediaSources.RemoveRange(allMediaSources);

            List<JellyfinLibrary> allJellyfinLibraries = await context.JellyfinLibraries
                .Where(l => mediaSourceIds.Contains(l.MediaSourceId))
                .ToListAsync();
            var libraryIds = allJellyfinLibraries.Map(l => l.Id).ToList();
            context.JellyfinLibraries.RemoveRange(allJellyfinLibraries);

            List<int> movieIds = await context.JellyfinMovies
                .Where(m => libraryIds.Contains(m.LibraryPath.LibraryId))
                .Map(pm => pm.Id)
                .ToListAsync();

            List<int> showIds = await context.JellyfinShows
                .Where(m => libraryIds.Contains(m.LibraryPath.LibraryId))
                .Map(ps => ps.Id)
                .ToListAsync();

            List<int> episodeIds = await context.JellyfinEpisodes
                .Where(m => libraryIds.Contains(m.LibraryPath.LibraryId))
                .Map(ps => ps.Id)
                .ToListAsync();

            await context.SaveChangesAsync();

            return movieIds.Append(showIds).Append(episodeIds).ToList();
        }

        public async Task<Unit> UpsertEmby(string address, string serverName, string operatingSystem)
        {
            await using TvContext dbContext = _dbContextFactory.CreateDbContext();
            Option<EmbyMediaSource> maybeExisting = dbContext.EmbyMediaSources
                .Include(ms => ms.Connections)
                .OrderBy(ms => ms.Id)
                .HeadOrNone();

            return await maybeExisting.Match(
                async embyMediaSource =>
                {
                    if (!embyMediaSource.Connections.Any())
                    {
                        embyMediaSource.Connections.Add(new EmbyConnection { Address = address });
                    }
                    else if (embyMediaSource.Connections.Head().Address != address)
                    {
                        embyMediaSource.Connections.Head().Address = address;
                    }

                    if (embyMediaSource.ServerName != serverName)
                    {
                        embyMediaSource.ServerName = serverName;
                    }

                    if (embyMediaSource.OperatingSystem != operatingSystem)
                    {
                        embyMediaSource.OperatingSystem = operatingSystem;
                    }

                    await dbContext.SaveChangesAsync();

                    return Unit.Default;
                },
                async () =>
                {
                    var mediaSource = new EmbyMediaSource
                    {
                        ServerName = serverName,
                        OperatingSystem = operatingSystem,
                        Connections = new List<EmbyConnection>
                        {
                            new() { Address = address }
                        },
                        PathReplacements = new List<EmbyPathReplacement>()
                    };

                    await dbContext.AddAsync(mediaSource);
                    await dbContext.SaveChangesAsync();

                    return Unit.Default;
                });
        }

        public Task<List<EmbyMediaSource>> GetAllEmby()
        {
            using TvContext context = _dbContextFactory.CreateDbContext();
            return context.EmbyMediaSources
                .Include(p => p.Connections)
                .ToListAsync();
        }

        public Task<Option<EmbyMediaSource>> GetEmby(int id)
        {
            using TvContext context = _dbContextFactory.CreateDbContext();
            return context.EmbyMediaSources
                .Include(p => p.Connections)
                .Include(p => p.Libraries)
                .Include(p => p.PathReplacements)
                .OrderBy(s => s.Id) // https://github.com/dotnet/efcore/issues/22579
                .SingleOrDefaultAsync(p => p.Id == id)
                .Map(Optional);
        }

        public async Task<Option<EmbyMediaSource>> GetEmbyByLibraryId(int embyLibraryId)
        {
            int? id = await _dbConnection.QuerySingleAsync<int?>(
                @"SELECT L.MediaSourceId FROM Library L
                INNER JOIN EmbyLibrary PL on L.Id = PL.Id
                WHERE L.Id = @EmbyLibraryId",
                new { EmbyLibraryId = embyLibraryId });

            await using TvContext context = _dbContextFactory.CreateDbContext();
            return await context.EmbyMediaSources
                .Include(p => p.Connections)
                .Include(p => p.Libraries)
                .OrderBy(p => p.Id)
                .SingleOrDefaultAsync(p => p.Id == id)
                .Map(Optional);
        }

        public Task<Option<EmbyLibrary>> GetEmbyLibrary(int embyLibraryId)
        {
            using TvContext context = _dbContextFactory.CreateDbContext();
            return context.EmbyLibraries
                .Include(l => l.Paths)
                .OrderBy(l => l.Id) // https://github.com/dotnet/efcore/issues/22579
                .SingleOrDefaultAsync(l => l.Id == embyLibraryId)
                .Map(Optional);
        }

        public Task<List<EmbyLibrary>> GetEmbyLibraries(int embyMediaSourceId)
        {
            using TvContext context = _dbContextFactory.CreateDbContext();
            return context.EmbyLibraries
                .Filter(l => l.MediaSourceId == embyMediaSourceId)
                .ToListAsync();
        }

        public Task<List<EmbyPathReplacement>> GetEmbyPathReplacements(int embyMediaSourceId)
        {
            using TvContext context = _dbContextFactory.CreateDbContext();
            return context.EmbyPathReplacements
                .Filter(r => r.EmbyMediaSourceId == embyMediaSourceId)
                .Include(jpr => jpr.EmbyMediaSource)
                .ToListAsync();
        }

        public Task<List<EmbyPathReplacement>> GetEmbyPathReplacementsByLibraryId(int embyLibraryPathId)
        {
            using TvContext context = _dbContextFactory.CreateDbContext();
            return context.EmbyPathReplacements
                .FromSqlRaw(
                    @"select epr.* from LibraryPath lp
                    inner join EmbyLibrary el ON el.Id = lp.LibraryId
                    inner join Library l ON l.Id = el.Id
                    inner join EmbyPathReplacement epr on epr.EmbyMediaSourceId = l.MediaSourceId
                    where lp.Id = {0}",
                    embyLibraryPathId)
                .Include(jpr => jpr.EmbyMediaSource)
                .ToListAsync();
        }

        public async Task<Unit> UpdatePathReplacements(
            int embyMediaSourceId,
            List<EmbyPathReplacement> toAdd,
            List<EmbyPathReplacement> toUpdate,
            List<EmbyPathReplacement> toDelete)
        {
            foreach (EmbyPathReplacement add in toAdd)
            {
                await _dbConnection.ExecuteAsync(
                    @"INSERT INTO EmbyPathReplacement
                    (EmbyPath, LocalPath, EmbyMediaSourceId)
                    VALUES (@EmbyPath, @LocalPath, @EmbyMediaSourceId)",
                    new { add.EmbyPath, add.LocalPath, EmbyMediaSourceId = embyMediaSourceId });
            }

            foreach (EmbyPathReplacement update in toUpdate)
            {
                await _dbConnection.ExecuteAsync(
                    @"UPDATE EmbyPathReplacement
                    SET EmbyPath = @EmbyPath, LocalPath = @LocalPath
                    WHERE Id = @Id",
                    new { update.EmbyPath, update.LocalPath, update.Id });
            }

            foreach (EmbyPathReplacement delete in toDelete)
            {
                await _dbConnection.ExecuteAsync(
                    @"DELETE FROM EmbyPathReplacement WHERE Id = @Id",
                    new { delete.Id });
            }

            return Unit.Default;
        }

        public async Task<List<int>> DeleteAllEmby()
        {
            await using TvContext context = _dbContextFactory.CreateDbContext();

            List<EmbyMediaSource> allMediaSources = await context.EmbyMediaSources.ToListAsync();
            var mediaSourceIds = allMediaSources.Map(ms => ms.Id).ToList();
            context.EmbyMediaSources.RemoveRange(allMediaSources);

            List<EmbyLibrary> allEmbyLibraries = await context.EmbyLibraries
                .Where(l => mediaSourceIds.Contains(l.MediaSourceId))
                .ToListAsync();
            var libraryIds = allEmbyLibraries.Map(l => l.Id).ToList();
            context.EmbyLibraries.RemoveRange(allEmbyLibraries);

            List<int> movieIds = await context.EmbyMovies
                .Where(m => libraryIds.Contains(m.LibraryPath.LibraryId))
                .Map(pm => pm.Id)
                .ToListAsync();

            List<int> showIds = await context.EmbyShows
                .Where(m => libraryIds.Contains(m.LibraryPath.LibraryId))
                .Map(ps => ps.Id)
                .ToListAsync();

            List<int> episodeIds = await context.EmbyEpisodes
                .Where(m => libraryIds.Contains(m.LibraryPath.LibraryId))
                .Map(ps => ps.Id)
                .ToListAsync();

            await context.SaveChangesAsync();

            return movieIds.Append(showIds).Append(episodeIds).ToList();
        }

        public Task<Unit> EnableEmbyLibrarySync(IEnumerable<int> libraryIds) =>
            _dbConnection.ExecuteAsync(
                "UPDATE EmbyLibrary SET ShouldSyncItems = 1 WHERE Id IN @ids",
                new { ids = libraryIds }).Map(_ => Unit.Default);

        public async Task<List<int>> DisableEmbyLibrarySync(List<int> libraryIds)
        {
            await _dbConnection.ExecuteAsync(
                "UPDATE EmbyLibrary SET ShouldSyncItems = 0 WHERE Id IN @ids",
                new { ids = libraryIds });

            await _dbConnection.ExecuteAsync(
                "UPDATE Library SET LastScan = null WHERE Id IN @ids",
                new { ids = libraryIds });

            List<int> movieIds = await _dbConnection.QueryAsync<int>(
                @"SELECT m.Id FROM MediaItem m
                INNER JOIN EmbyMovie pm ON pm.Id = m.Id
                INNER JOIN LibraryPath lp ON lp.Id = m.LibraryPathId
                INNER JOIN Library l ON l.Id = lp.LibraryId
                WHERE l.Id IN @ids",
                new { ids = libraryIds }).Map(result => result.ToList());

            await _dbConnection.ExecuteAsync(
                @"DELETE FROM MediaItem WHERE Id IN
                (SELECT m.Id FROM MediaItem m
                INNER JOIN EmbyMovie pm ON pm.Id = m.Id
                INNER JOIN LibraryPath lp ON lp.Id = m.LibraryPathId
                INNER JOIN Library l ON l.Id = lp.LibraryId
                WHERE l.Id IN @ids)",
                new { ids = libraryIds });

            List<int> episodeIds = await _dbConnection.QueryAsync<int>(
                @"SELECT m.Id FROM MediaItem m
                INNER JOIN EmbyEpisode pe ON pe.Id = m.Id
                INNER JOIN LibraryPath lp ON lp.Id = m.LibraryPathId
                INNER JOIN Library l ON l.Id = lp.LibraryId
                WHERE l.Id IN @ids",
                new { ids = libraryIds }).Map(result => result.ToList());

            await _dbConnection.ExecuteAsync(
                @"DELETE FROM MediaItem WHERE Id IN
                (SELECT m.Id FROM MediaItem m
                INNER JOIN EmbyEpisode pe ON pe.Id = m.Id
                INNER JOIN LibraryPath lp ON lp.Id = m.LibraryPathId
                INNER JOIN Library l ON l.Id = lp.LibraryId
                WHERE l.Id IN @ids)",
                new { ids = libraryIds });

            List<int> seasonIds = await _dbConnection.QueryAsync<int>(
                @"SELECT m.Id FROM MediaItem m
                INNER JOIN EmbySeason es ON es.Id = m.Id
                INNER JOIN LibraryPath lp ON lp.Id = m.LibraryPathId
                INNER JOIN Library l ON l.Id = lp.LibraryId
                WHERE l.Id IN @ids",
                new { ids = libraryIds }).Map(result => result.ToList());

            await _dbConnection.ExecuteAsync(
                @"DELETE FROM MediaItem WHERE Id IN
                (SELECT m.Id FROM MediaItem m
                INNER JOIN EmbySeason ps ON ps.Id = m.Id
                INNER JOIN LibraryPath lp ON lp.Id = m.LibraryPathId
                INNER JOIN Library l ON l.Id = lp.LibraryId
                WHERE l.Id IN @ids)",
                new { ids = libraryIds });

            List<int> showIds = await _dbConnection.QueryAsync<int>(
                @"SELECT m.Id FROM MediaItem m
                INNER JOIN EmbyShow ps ON ps.Id = m.Id
                INNER JOIN LibraryPath lp ON lp.Id = m.LibraryPathId
                INNER JOIN Library l ON l.Id = lp.LibraryId
                WHERE l.Id IN @ids",
                new { ids = libraryIds }).Map(result => result.ToList());

            await _dbConnection.ExecuteAsync(
                @"DELETE FROM MediaItem WHERE Id IN
                (SELECT m.Id FROM MediaItem m
                INNER JOIN EmbyShow ps ON ps.Id = m.Id
                INNER JOIN LibraryPath lp ON lp.Id = m.LibraryPathId
                INNER JOIN Library l ON l.Id = lp.LibraryId
                WHERE l.Id IN @ids)",
                new { ids = libraryIds });

            return movieIds.Append(showIds).Append(seasonIds).Append(episodeIds).ToList();
        }
    }
}
