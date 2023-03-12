using ErsatzTV.Core.Domain;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Libraries;

public class GetExternalCollectionsHandler : IRequestHandler<GetExternalCollections, List<LibraryViewModel>>
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;

    public GetExternalCollectionsHandler(IDbContextFactory<TvContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<List<LibraryViewModel>> Handle(
        GetExternalCollections request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        List<int> mediaSourceIds = await dbContext.EmbyMediaSources
            .Filter(ems => ems.Libraries.Any(l => ((EmbyLibrary)l).ShouldSyncItems))
            .Map(ems => ems.Id)
            .ToListAsync(cancellationToken: cancellationToken);

        return mediaSourceIds.Map(
                id => new LibraryViewModel(
                    "Emby",
                    0,
                    "Collections",
                    0,
                    id))
            .ToList();
    }
}
