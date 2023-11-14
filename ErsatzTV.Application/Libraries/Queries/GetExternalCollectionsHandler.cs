using ErsatzTV.Core.Domain;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Libraries;

public class GetExternalCollectionsHandler : IRequestHandler<GetExternalCollections, List<LibraryViewModel>>
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;

    public GetExternalCollectionsHandler(IDbContextFactory<TvContext> dbContextFactory) =>
        _dbContextFactory = dbContextFactory;

    public async Task<List<LibraryViewModel>> Handle(
        GetExternalCollections request,
        CancellationToken cancellationToken)
    {
        List<LibraryViewModel> result = new();

        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        result.AddRange(await GetEmbyExternalCollections(dbContext, cancellationToken));
        result.AddRange(await GetJellyfinExternalCollections(dbContext, cancellationToken));
        result.AddRange(await GetPlexExternalCollections(dbContext, cancellationToken));

        return result;
    }

    private static async Task<IEnumerable<LibraryViewModel>> GetEmbyExternalCollections(
        TvContext dbContext,
        CancellationToken cancellationToken)
    {
        List<int> embyMediaSourceIds = await dbContext.EmbyMediaSources
            .Filter(ems => ems.Libraries.Any(l => ((EmbyLibrary)l).ShouldSyncItems))
            .Map(ems => ems.Id)
            .ToListAsync(cancellationToken);

        return embyMediaSourceIds.Map(id => new LibraryViewModel("Emby", 0, "Collections", 0, id, string.Empty));
    }
    
    private static async Task<IEnumerable<LibraryViewModel>> GetJellyfinExternalCollections(
        TvContext dbContext,
        CancellationToken cancellationToken)
    {
        List<int> jellyfinMediaSourceIds = await dbContext.JellyfinMediaSources
            .Filter(jms => jms.Libraries.Any(l => ((JellyfinLibrary)l).ShouldSyncItems))
            .Map(jms => jms.Id)
            .ToListAsync(cancellationToken);

        return jellyfinMediaSourceIds.Map(
            id => new LibraryViewModel("Jellyfin", 0, "Collections", 0, id, string.Empty));
    }
    
    private static async Task<IEnumerable<LibraryViewModel>> GetPlexExternalCollections(
        TvContext dbContext,
        CancellationToken cancellationToken)
    {
        List<int> plexMediaSourceIds = await dbContext.PlexMediaSources
            .Filter(pms => pms.Libraries.Any(l => ((PlexLibrary)l).ShouldSyncItems))
            .Map(pms => pms.Id)
            .ToListAsync(cancellationToken);

        return plexMediaSourceIds.Map(
            id => new LibraryViewModel("Plex", 0, "Collections", 0, id, string.Empty));
    }
}
