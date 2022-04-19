using Dapper;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Infrastructure.Data.Repositories;

public class MediaSourceRepository : IMediaSourceRepository
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;

    public MediaSourceRepository(IDbContextFactory<TvContext> dbContextFactory) => _dbContextFactory = dbContextFactory;

    public async Task<PlexMediaSource> Add(PlexMediaSource plexMediaSource)
    {
        await using TvContext context = await _dbContextFactory.CreateDbContextAsync();
        await context.PlexMediaSources.AddAsync(plexMediaSource);
        await context.SaveChangesAsync();
        return plexMediaSource;
    }

    public async Task<List<PlexMediaSource>> GetAllPlex()
    {
        await using TvContext context = await _dbContextFactory.CreateDbContextAsync();
        return await context.PlexMediaSources
            .Include(p => p.Connections)
            .ToListAsync();
    }

    public async Task<List<PlexLibrary>> GetPlexLibraries(int plexMediaSourceId)
    {
        await using TvContext context = await _dbContextFactory.CreateDbContextAsync();
        return await context.PlexLibraries
            .Filter(l => l.MediaSourceId == plexMediaSourceId)
            .ToListAsync();
    }

    public async Task<List<PlexPathReplacement>> GetPlexPathReplacements(int plexMediaSourceId)
    {
        await using TvContext context = await _dbContextFactory.CreateDbContextAsync();
        return await context.PlexPathReplacements
            .Include(ppr => ppr.PlexMediaSource)
            .Filter(r => r.PlexMediaSourceId == plexMediaSourceId)
            .ToListAsync();
    }

    public async Task<Option<PlexLibrary>> GetPlexLibrary(int plexLibraryId)
    {
        await using TvContext context = await _dbContextFactory.CreateDbContextAsync();
        return await context.PlexLibraries
            .Include(l => l.Paths)
            .OrderBy(l => l.Id) // https://github.com/dotnet/efcore/issues/22579
            .SingleOrDefaultAsync(l => l.Id == plexLibraryId)
            .Map(Optional);
    }

    public async Task<Option<PlexMediaSource>> GetPlex(int id)
    {
        await using TvContext context = await _dbContextFactory.CreateDbContextAsync();
        return await context.PlexMediaSources
            .Include(p => p.Connections)
            .Include(p => p.Libraries)
            .Include(p => p.PathReplacements)
            .OrderBy(s => s.Id) // https://github.com/dotnet/efcore/issues/22579
            .SingleOrDefaultAsync(p => p.Id == id)
            .Map(Optional);
    }

    public async Task<Option<PlexMediaSource>> GetPlexByLibraryId(int plexLibraryId)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        int? id = await dbContext.Connection.QuerySingleOrDefaultAsync<int?>(
            @"SELECT L.MediaSourceId FROM Library L
                INNER JOIN PlexLibrary PL on L.Id = PL.Id
                WHERE L.Id = @PlexLibraryId",
            new { PlexLibraryId = plexLibraryId });

        return await dbContext.PlexMediaSources
            .Include(p => p.Connections)
            .Include(p => p.Libraries)
            .OrderBy(p => p.Id)
            .SingleOrDefaultAsync(p => p.Id == id)
            .Map(Optional);
    }

    public async Task<List<PlexPathReplacement>> GetPlexPathReplacementsByLibraryId(int plexLibraryPathId)
    {
        await using TvContext context = await _dbContextFactory.CreateDbContextAsync();
        return await context.PlexPathReplacements
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
        List<PlexConnection> toAdd,
        List<PlexConnection> toDelete)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        dbContext.Entry(plexMediaSource).State = EntityState.Modified;

        if (toAdd.Any() || toDelete.Any())
        {
            plexMediaSource.Connections.Clear();
            await dbContext.Entry(plexMediaSource).Collection(pms => pms.Connections).LoadAsync();

            plexMediaSource.Connections.AddRange(toAdd);
            plexMediaSource.Connections.RemoveAll(toDelete.Contains);
        }
        else
        {
            foreach (PlexConnection connection in plexMediaSource.Connections)
            {
                dbContext.Entry(connection).State = EntityState.Modified;
            }
        }

        await dbContext.SaveChangesAsync();
    }

    public async Task<List<int>> UpdateLibraries(
        int plexMediaSourceId,
        List<PlexLibrary> toAdd,
        List<PlexLibrary> toDelete)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();

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
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();

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
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();

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
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        foreach (PlexPathReplacement add in toAdd)
        {
            await dbContext.Connection.ExecuteAsync(
                @"INSERT INTO PlexPathReplacement
                    (PlexPath, LocalPath, PlexMediaSourceId)
                    VALUES (@PlexPath, @LocalPath, @PlexMediaSourceId)",
                new { add.PlexPath, add.LocalPath, PlexMediaSourceId = plexMediaSourceId });
        }

        foreach (PlexPathReplacement update in toUpdate)
        {
            await dbContext.Connection.ExecuteAsync(
                @"UPDATE PlexPathReplacement
                    SET PlexPath = @PlexPath, LocalPath = @LocalPath
                    WHERE Id = @Id",
                new { update.PlexPath, update.LocalPath, update.Id });
        }

        foreach (PlexPathReplacement delete in toDelete)
        {
            await dbContext.Connection.ExecuteAsync(
                @"DELETE FROM PlexPathReplacement WHERE Id = @Id",
                new { delete.Id });
        }

        return Unit.Default;
    }

    public async Task<List<int>> DeleteAllPlex()
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        List<PlexMediaSource> allMediaSources = await dbContext.PlexMediaSources.ToListAsync();
        dbContext.PlexMediaSources.RemoveRange(allMediaSources);

        List<PlexLibrary> allPlexLibraries = await dbContext.PlexLibraries.ToListAsync();
        dbContext.PlexLibraries.RemoveRange(allPlexLibraries);

        List<int> movieIds = await dbContext.PlexMovies.Map(pm => pm.Id).ToListAsync();
        List<int> showIds = await dbContext.PlexShows.Map(ps => ps.Id).ToListAsync();
        List<int> episodeIds = await dbContext.PlexEpisodes.Map(pe => pe.Id).ToListAsync();

        await dbContext.SaveChangesAsync();

        return movieIds.Append(showIds).Append(episodeIds).ToList();
    }

    public async Task<List<int>> DeletePlex(PlexMediaSource plexMediaSource)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        List<int> mediaItemIds = await dbContext.Connection.QueryAsync<int>(
                @"SELECT MediaItem.Id FROM MediaItem
                  INNER JOIN LibraryPath LP on MediaItem.LibraryPathId = LP.Id
                  INNER JOIN Library L on LP.LibraryId = L.Id
                  WHERE L.MediaSourceId = @PlexMediaSourceId",
                new { PlexMediaSourceId = plexMediaSource.Id })
            .Map(result => result.ToList());

        await dbContext.Connection.ExecuteAsync(
            @"DELETE FROM MediaSource WHERE Id = @PlexMediaSourceId",
            new { PlexMediaSourceId = plexMediaSource.Id });

        return mediaItemIds;
    }

    public async Task<List<int>> DisablePlexLibrarySync(List<int> libraryIds)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        await dbContext.Connection.ExecuteAsync(
            "UPDATE PlexLibrary SET ShouldSyncItems = 0 WHERE Id IN @ids",
            new { ids = libraryIds });

        await dbContext.Connection.ExecuteAsync(
            "UPDATE Library SET LastScan = null WHERE Id IN @ids",
            new { ids = libraryIds });

        List<int> movieIds = await dbContext.Connection.QueryAsync<int>(
            @"SELECT m.Id FROM MediaItem m
                INNER JOIN PlexMovie pm ON pm.Id = m.Id
                INNER JOIN LibraryPath lp ON lp.Id = m.LibraryPathId
                INNER JOIN Library l ON l.Id = lp.LibraryId
                WHERE l.Id IN @ids",
            new { ids = libraryIds }).Map(result => result.ToList());

        await dbContext.Connection.ExecuteAsync(
            @"DELETE FROM MediaItem WHERE Id IN
                (SELECT m.Id FROM MediaItem m
                INNER JOIN PlexMovie pm ON pm.Id = m.Id
                INNER JOIN LibraryPath lp ON lp.Id = m.LibraryPathId
                INNER JOIN Library l ON l.Id = lp.LibraryId
                WHERE l.Id IN @ids)",
            new { ids = libraryIds });

        List<int> episodeIds = await dbContext.Connection.QueryAsync<int>(
            @"SELECT m.Id FROM MediaItem m
                INNER JOIN PlexEpisode pe ON pe.Id = m.Id
                INNER JOIN LibraryPath lp ON lp.Id = m.LibraryPathId
                INNER JOIN Library l ON l.Id = lp.LibraryId
                WHERE l.Id IN @ids",
            new { ids = libraryIds }).Map(result => result.ToList());

        await dbContext.Connection.ExecuteAsync(
            @"DELETE FROM MediaItem WHERE Id IN
                (SELECT m.Id FROM MediaItem m
                INNER JOIN PlexEpisode pe ON pe.Id = m.Id
                INNER JOIN LibraryPath lp ON lp.Id = m.LibraryPathId
                INNER JOIN Library l ON l.Id = lp.LibraryId
                WHERE l.Id IN @ids)",
            new { ids = libraryIds });

        List<int> seasonIds = await dbContext.Connection.QueryAsync<int>(
            @"SELECT m.Id FROM MediaItem m
                INNER JOIN PlexSeason ps ON ps.Id = m.Id
                INNER JOIN LibraryPath lp ON lp.Id = m.LibraryPathId
                INNER JOIN Library l ON l.Id = lp.LibraryId
                WHERE l.Id IN @ids",
            new { ids = libraryIds }).Map(result => result.ToList());

        await dbContext.Connection.ExecuteAsync(
            @"DELETE FROM MediaItem WHERE Id IN
                (SELECT m.Id FROM MediaItem m
                INNER JOIN PlexSeason ps ON ps.Id = m.Id
                INNER JOIN LibraryPath lp ON lp.Id = m.LibraryPathId
                INNER JOIN Library l ON l.Id = lp.LibraryId
                WHERE l.Id IN @ids)",
            new { ids = libraryIds });

        List<int> showIds = await dbContext.Connection.QueryAsync<int>(
            @"SELECT m.Id FROM MediaItem m
                INNER JOIN PlexShow ps ON ps.Id = m.Id
                INNER JOIN LibraryPath lp ON lp.Id = m.LibraryPathId
                INNER JOIN Library l ON l.Id = lp.LibraryId
                WHERE l.Id IN @ids",
            new { ids = libraryIds }).Map(result => result.ToList());

        await dbContext.Connection.ExecuteAsync(
            @"DELETE FROM MediaItem WHERE Id IN
                (SELECT m.Id FROM MediaItem m
                INNER JOIN PlexShow ps ON ps.Id = m.Id
                INNER JOIN LibraryPath lp ON lp.Id = m.LibraryPathId
                INNER JOIN Library l ON l.Id = lp.LibraryId
                WHERE l.Id IN @ids)",
            new { ids = libraryIds });

        return movieIds.Append(showIds).Append(seasonIds).Append(episodeIds).ToList();
    }

    public async Task EnablePlexLibrarySync(IEnumerable<int> libraryIds)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        await dbContext.Connection.ExecuteAsync(
            "UPDATE PlexLibrary SET ShouldSyncItems = 1 WHERE Id IN @ids",
            new { ids = libraryIds });
    }

    public async Task<Unit> UpsertJellyfin(string address, string serverName, string operatingSystem)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
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

    public async Task<List<JellyfinMediaSource>> GetAllJellyfin()
    {
        await using TvContext context = await _dbContextFactory.CreateDbContextAsync();
        return await context.JellyfinMediaSources
            .Include(p => p.Connections)
            .ToListAsync();
    }

    public async Task<Option<JellyfinMediaSource>> GetJellyfin(int id)
    {
        await using TvContext context = await _dbContextFactory.CreateDbContextAsync();
        return await context.JellyfinMediaSources
            .Include(p => p.Connections)
            .Include(p => p.Libraries)
            .Include(p => p.PathReplacements)
            .OrderBy(s => s.Id) // https://github.com/dotnet/efcore/issues/22579
            .SingleOrDefaultAsync(p => p.Id == id)
            .Map(Optional);
    }

    public async Task<List<JellyfinLibrary>> GetJellyfinLibraries(int jellyfinMediaSourceId)
    {
        await using TvContext context = await _dbContextFactory.CreateDbContextAsync();
        return await context.JellyfinLibraries
            .Filter(l => l.MediaSourceId == jellyfinMediaSourceId)
            .ToListAsync();
    }

    public async Task<Unit> EnableJellyfinLibrarySync(IEnumerable<int> libraryIds)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.Connection.ExecuteAsync(
            "UPDATE JellyfinLibrary SET ShouldSyncItems = 1 WHERE Id IN @ids",
            new { ids = libraryIds }).ToUnit();
    }

    public async Task<List<int>> DisableJellyfinLibrarySync(List<int> libraryIds)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        await dbContext.Connection.ExecuteAsync(
            "UPDATE JellyfinLibrary SET ShouldSyncItems = 0 WHERE Id IN @ids",
            new { ids = libraryIds });

        await dbContext.Connection.ExecuteAsync(
            "UPDATE Library SET LastScan = null WHERE Id IN @ids",
            new { ids = libraryIds });

        List<int> movieIds = await dbContext.Connection.QueryAsync<int>(
            @"SELECT m.Id FROM MediaItem m
                INNER JOIN JellyfinMovie pm ON pm.Id = m.Id
                INNER JOIN LibraryPath lp ON lp.Id = m.LibraryPathId
                INNER JOIN Library l ON l.Id = lp.LibraryId
                WHERE l.Id IN @ids",
            new { ids = libraryIds }).Map(result => result.ToList());

        await dbContext.Connection.ExecuteAsync(
            @"DELETE FROM MediaItem WHERE Id IN
                (SELECT m.Id FROM MediaItem m
                INNER JOIN JellyfinMovie pm ON pm.Id = m.Id
                INNER JOIN LibraryPath lp ON lp.Id = m.LibraryPathId
                INNER JOIN Library l ON l.Id = lp.LibraryId
                WHERE l.Id IN @ids)",
            new { ids = libraryIds });

        List<int> episodeIds = await dbContext.Connection.QueryAsync<int>(
            @"SELECT m.Id FROM MediaItem m
                INNER JOIN JellyfinEpisode pe ON pe.Id = m.Id
                INNER JOIN LibraryPath lp ON lp.Id = m.LibraryPathId
                INNER JOIN Library l ON l.Id = lp.LibraryId
                WHERE l.Id IN @ids",
            new { ids = libraryIds }).Map(result => result.ToList());

        await dbContext.Connection.ExecuteAsync(
            @"DELETE FROM MediaItem WHERE Id IN
                (SELECT m.Id FROM MediaItem m
                INNER JOIN JellyfinEpisode pe ON pe.Id = m.Id
                INNER JOIN LibraryPath lp ON lp.Id = m.LibraryPathId
                INNER JOIN Library l ON l.Id = lp.LibraryId
                WHERE l.Id IN @ids)",
            new { ids = libraryIds });

        List<int> seasonIds = await dbContext.Connection.QueryAsync<int>(
            @"SELECT m.Id FROM MediaItem m
                INNER JOIN JellyfinSeason js ON js.Id = m.Id
                INNER JOIN LibraryPath lp ON lp.Id = m.LibraryPathId
                INNER JOIN Library l ON l.Id = lp.LibraryId
                WHERE l.Id IN @ids",
            new { ids = libraryIds }).Map(result => result.ToList());

        await dbContext.Connection.ExecuteAsync(
            @"DELETE FROM MediaItem WHERE Id IN
                (SELECT m.Id FROM MediaItem m
                INNER JOIN JellyfinSeason ps ON ps.Id = m.Id
                INNER JOIN LibraryPath lp ON lp.Id = m.LibraryPathId
                INNER JOIN Library l ON l.Id = lp.LibraryId
                WHERE l.Id IN @ids)",
            new { ids = libraryIds });

        List<int> showIds = await dbContext.Connection.QueryAsync<int>(
            @"SELECT m.Id FROM MediaItem m
                INNER JOIN JellyfinShow ps ON ps.Id = m.Id
                INNER JOIN LibraryPath lp ON lp.Id = m.LibraryPathId
                INNER JOIN Library l ON l.Id = lp.LibraryId
                WHERE l.Id IN @ids",
            new { ids = libraryIds }).Map(result => result.ToList());

        await dbContext.Connection.ExecuteAsync(
            @"DELETE FROM MediaItem WHERE Id IN
                (SELECT m.Id FROM MediaItem m
                INNER JOIN JellyfinShow ps ON ps.Id = m.Id
                INNER JOIN LibraryPath lp ON lp.Id = m.LibraryPathId
                INNER JOIN Library l ON l.Id = lp.LibraryId
                WHERE l.Id IN @ids)",
            new { ids = libraryIds });

        return movieIds.Append(showIds).Append(seasonIds).Append(episodeIds).ToList();
    }

    public async Task<Option<JellyfinLibrary>> GetJellyfinLibrary(int jellyfinLibraryId)
    {
        await using TvContext context = await _dbContextFactory.CreateDbContextAsync();
        return await context.JellyfinLibraries
            .Include(l => l.Paths)
            .OrderBy(l => l.Id) // https://github.com/dotnet/efcore/issues/22579
            .SingleOrDefaultAsync(l => l.Id == jellyfinLibraryId)
            .Map(Optional);
    }

    public async Task<Option<JellyfinMediaSource>> GetJellyfinByLibraryId(int jellyfinLibraryId)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        int? id = await dbContext.Connection.QuerySingleOrDefaultAsync<int?>(
            @"SELECT L.MediaSourceId FROM Library L
                INNER JOIN JellyfinLibrary PL on L.Id = PL.Id
                WHERE L.Id = @JellyfinLibraryId",
            new { JellyfinLibraryId = jellyfinLibraryId });

        return await dbContext.JellyfinMediaSources
            .Include(p => p.Connections)
            .Include(p => p.Libraries)
            .OrderBy(p => p.Id)
            .SingleOrDefaultAsync(p => p.Id == id)
            .Map(Optional);
    }

    public async Task<List<JellyfinPathReplacement>> GetJellyfinPathReplacements(int jellyfinMediaSourceId)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.JellyfinPathReplacements
            .Filter(r => r.JellyfinMediaSourceId == jellyfinMediaSourceId)
            .Include(jpr => jpr.JellyfinMediaSource)
            .ToListAsync();
    }

    public async Task<List<JellyfinPathReplacement>> GetJellyfinPathReplacementsByLibraryId(int jellyfinLibraryPathId)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.JellyfinPathReplacements
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
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        foreach (JellyfinPathReplacement add in toAdd)
        {
            await dbContext.Connection.ExecuteAsync(
                @"INSERT INTO JellyfinPathReplacement
                    (JellyfinPath, LocalPath, JellyfinMediaSourceId)
                    VALUES (@JellyfinPath, @LocalPath, @JellyfinMediaSourceId)",
                new { add.JellyfinPath, add.LocalPath, JellyfinMediaSourceId = jellyfinMediaSourceId });
        }

        foreach (JellyfinPathReplacement update in toUpdate)
        {
            await dbContext.Connection.ExecuteAsync(
                @"UPDATE JellyfinPathReplacement
                    SET JellyfinPath = @JellyfinPath, LocalPath = @LocalPath
                    WHERE Id = @Id",
                new { update.JellyfinPath, update.LocalPath, update.Id });
        }

        foreach (JellyfinPathReplacement delete in toDelete)
        {
            await dbContext.Connection.ExecuteAsync(
                @"DELETE FROM JellyfinPathReplacement WHERE Id = @Id",
                new { delete.Id });
        }

        return Unit.Default;
    }

    public async Task<List<int>> DeleteAllJellyfin()
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        List<JellyfinMediaSource> allMediaSources = await dbContext.JellyfinMediaSources.ToListAsync();
        var mediaSourceIds = allMediaSources.Map(ms => ms.Id).ToList();
        dbContext.JellyfinMediaSources.RemoveRange(allMediaSources);

        List<JellyfinLibrary> allJellyfinLibraries = await dbContext.JellyfinLibraries
            .Where(l => mediaSourceIds.Contains(l.MediaSourceId))
            .ToListAsync();
        var libraryIds = allJellyfinLibraries.Map(l => l.Id).ToList();
        dbContext.JellyfinLibraries.RemoveRange(allJellyfinLibraries);

        List<int> movieIds = await dbContext.JellyfinMovies
            .Where(m => libraryIds.Contains(m.LibraryPath.LibraryId))
            .Map(pm => pm.Id)
            .ToListAsync();

        List<int> showIds = await dbContext.JellyfinShows
            .Where(m => libraryIds.Contains(m.LibraryPath.LibraryId))
            .Map(ps => ps.Id)
            .ToListAsync();

        List<int> episodeIds = await dbContext.JellyfinEpisodes
            .Where(m => libraryIds.Contains(m.LibraryPath.LibraryId))
            .Map(ps => ps.Id)
            .ToListAsync();

        await dbContext.SaveChangesAsync();

        return movieIds.Append(showIds).Append(episodeIds).ToList();
    }

    public async Task<Unit> UpsertEmby(string address, string serverName, string operatingSystem)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
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

    public async Task<List<EmbyMediaSource>> GetAllEmby()
    {
        await using TvContext context = await _dbContextFactory.CreateDbContextAsync();
        return await context.EmbyMediaSources
            .Include(p => p.Connections)
            .ToListAsync();
    }

    public async Task<Option<EmbyMediaSource>> GetEmby(int id)
    {
        await using TvContext context = await _dbContextFactory.CreateDbContextAsync();
        return await context.EmbyMediaSources
            .Include(p => p.Connections)
            .Include(p => p.Libraries)
            .Include(p => p.PathReplacements)
            .OrderBy(s => s.Id) // https://github.com/dotnet/efcore/issues/22579
            .SingleOrDefaultAsync(p => p.Id == id)
            .Map(Optional);
    }

    public async Task<Option<EmbyMediaSource>> GetEmbyByLibraryId(int embyLibraryId)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        int? id = await dbContext.Connection.QuerySingleOrDefaultAsync<int?>(
            @"SELECT L.MediaSourceId FROM Library L
                INNER JOIN EmbyLibrary PL on L.Id = PL.Id
                WHERE L.Id = @EmbyLibraryId",
            new { EmbyLibraryId = embyLibraryId });

        return await dbContext.EmbyMediaSources
            .Include(p => p.Connections)
            .Include(p => p.Libraries)
            .OrderBy(p => p.Id)
            .SingleOrDefaultAsync(p => p.Id == id)
            .Map(Optional);
    }

    public async Task<Option<EmbyLibrary>> GetEmbyLibrary(int embyLibraryId)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.EmbyLibraries
            .Include(l => l.Paths)
            .OrderBy(l => l.Id) // https://github.com/dotnet/efcore/issues/22579
            .SingleOrDefaultAsync(l => l.Id == embyLibraryId)
            .Map(Optional);
    }

    public async Task<List<EmbyLibrary>> GetEmbyLibraries(int embyMediaSourceId)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.EmbyLibraries
            .Filter(l => l.MediaSourceId == embyMediaSourceId)
            .ToListAsync();
    }

    public async Task<List<EmbyPathReplacement>> GetEmbyPathReplacements(int embyMediaSourceId)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.EmbyPathReplacements
            .Filter(r => r.EmbyMediaSourceId == embyMediaSourceId)
            .Include(jpr => jpr.EmbyMediaSource)
            .ToListAsync();
    }

    public async Task<List<EmbyPathReplacement>> GetEmbyPathReplacementsByLibraryId(int embyLibraryPathId)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.EmbyPathReplacements
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
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        foreach (EmbyPathReplacement add in toAdd)
        {
            await dbContext.Connection.ExecuteAsync(
                @"INSERT INTO EmbyPathReplacement
                    (EmbyPath, LocalPath, EmbyMediaSourceId)
                    VALUES (@EmbyPath, @LocalPath, @EmbyMediaSourceId)",
                new { add.EmbyPath, add.LocalPath, EmbyMediaSourceId = embyMediaSourceId });
        }

        foreach (EmbyPathReplacement update in toUpdate)
        {
            await dbContext.Connection.ExecuteAsync(
                @"UPDATE EmbyPathReplacement
                    SET EmbyPath = @EmbyPath, LocalPath = @LocalPath
                    WHERE Id = @Id",
                new { update.EmbyPath, update.LocalPath, update.Id });
        }

        foreach (EmbyPathReplacement delete in toDelete)
        {
            await dbContext.Connection.ExecuteAsync(
                @"DELETE FROM EmbyPathReplacement WHERE Id = @Id",
                new { delete.Id });
        }

        return Unit.Default;
    }

    public async Task<List<int>> DeleteAllEmby()
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        List<EmbyMediaSource> allMediaSources = await dbContext.EmbyMediaSources.ToListAsync();
        var mediaSourceIds = allMediaSources.Map(ms => ms.Id).ToList();
        dbContext.EmbyMediaSources.RemoveRange(allMediaSources);

        List<EmbyLibrary> allEmbyLibraries = await dbContext.EmbyLibraries
            .Where(l => mediaSourceIds.Contains(l.MediaSourceId))
            .ToListAsync();
        var libraryIds = allEmbyLibraries.Map(l => l.Id).ToList();
        dbContext.EmbyLibraries.RemoveRange(allEmbyLibraries);

        List<int> movieIds = await dbContext.EmbyMovies
            .Where(m => libraryIds.Contains(m.LibraryPath.LibraryId))
            .Map(pm => pm.Id)
            .ToListAsync();

        List<int> showIds = await dbContext.EmbyShows
            .Where(m => libraryIds.Contains(m.LibraryPath.LibraryId))
            .Map(ps => ps.Id)
            .ToListAsync();

        List<int> episodeIds = await dbContext.EmbyEpisodes
            .Where(m => libraryIds.Contains(m.LibraryPath.LibraryId))
            .Map(ps => ps.Id)
            .ToListAsync();

        await dbContext.SaveChangesAsync();

        return movieIds.Append(showIds).Append(episodeIds).ToList();
    }

    public async Task<Unit> EnableEmbyLibrarySync(IEnumerable<int> libraryIds)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.Connection.ExecuteAsync(
            "UPDATE EmbyLibrary SET ShouldSyncItems = 1 WHERE Id IN @ids",
            new { ids = libraryIds }).Map(_ => Unit.Default);
    }

    public async Task<List<int>> DisableEmbyLibrarySync(List<int> libraryIds)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        await dbContext.Connection.ExecuteAsync(
            "UPDATE EmbyLibrary SET ShouldSyncItems = 0 WHERE Id IN @ids",
            new { ids = libraryIds });

        await dbContext.Connection.ExecuteAsync(
            "UPDATE Library SET LastScan = null WHERE Id IN @ids",
            new { ids = libraryIds });

        List<int> movieIds = await dbContext.Connection.QueryAsync<int>(
            @"SELECT m.Id FROM MediaItem m
                INNER JOIN EmbyMovie pm ON pm.Id = m.Id
                INNER JOIN LibraryPath lp ON lp.Id = m.LibraryPathId
                INNER JOIN Library l ON l.Id = lp.LibraryId
                WHERE l.Id IN @ids",
            new { ids = libraryIds }).Map(result => result.ToList());

        await dbContext.Connection.ExecuteAsync(
            @"DELETE FROM MediaItem WHERE Id IN
                (SELECT m.Id FROM MediaItem m
                INNER JOIN EmbyMovie pm ON pm.Id = m.Id
                INNER JOIN LibraryPath lp ON lp.Id = m.LibraryPathId
                INNER JOIN Library l ON l.Id = lp.LibraryId
                WHERE l.Id IN @ids)",
            new { ids = libraryIds });

        List<int> episodeIds = await dbContext.Connection.QueryAsync<int>(
            @"SELECT m.Id FROM MediaItem m
                INNER JOIN EmbyEpisode pe ON pe.Id = m.Id
                INNER JOIN LibraryPath lp ON lp.Id = m.LibraryPathId
                INNER JOIN Library l ON l.Id = lp.LibraryId
                WHERE l.Id IN @ids",
            new { ids = libraryIds }).Map(result => result.ToList());

        await dbContext.Connection.ExecuteAsync(
            @"DELETE FROM MediaItem WHERE Id IN
                (SELECT m.Id FROM MediaItem m
                INNER JOIN EmbyEpisode pe ON pe.Id = m.Id
                INNER JOIN LibraryPath lp ON lp.Id = m.LibraryPathId
                INNER JOIN Library l ON l.Id = lp.LibraryId
                WHERE l.Id IN @ids)",
            new { ids = libraryIds });

        List<int> seasonIds = await dbContext.Connection.QueryAsync<int>(
            @"SELECT m.Id FROM MediaItem m
                INNER JOIN EmbySeason es ON es.Id = m.Id
                INNER JOIN LibraryPath lp ON lp.Id = m.LibraryPathId
                INNER JOIN Library l ON l.Id = lp.LibraryId
                WHERE l.Id IN @ids",
            new { ids = libraryIds }).Map(result => result.ToList());

        await dbContext.Connection.ExecuteAsync(
            @"DELETE FROM MediaItem WHERE Id IN
                (SELECT m.Id FROM MediaItem m
                INNER JOIN EmbySeason ps ON ps.Id = m.Id
                INNER JOIN LibraryPath lp ON lp.Id = m.LibraryPathId
                INNER JOIN Library l ON l.Id = lp.LibraryId
                WHERE l.Id IN @ids)",
            new { ids = libraryIds });

        List<int> showIds = await dbContext.Connection.QueryAsync<int>(
            @"SELECT m.Id FROM MediaItem m
                INNER JOIN EmbyShow ps ON ps.Id = m.Id
                INNER JOIN LibraryPath lp ON lp.Id = m.LibraryPathId
                INNER JOIN Library l ON l.Id = lp.LibraryId
                WHERE l.Id IN @ids",
            new { ids = libraryIds }).Map(result => result.ToList());

        await dbContext.Connection.ExecuteAsync(
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
