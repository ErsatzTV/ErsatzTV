using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using static ErsatzTV.Application.Plex.Mapper;

namespace ErsatzTV.Application.Plex;

public class GetPlexLibrariesBySourceIdHandler : IRequestHandler<GetPlexLibrariesBySourceId, List<PlexLibraryViewModel>>
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;

    public GetPlexLibrariesBySourceIdHandler(IDbContextFactory<TvContext> dbContextFactory) =>
        _dbContextFactory = dbContextFactory;

    public async Task<List<PlexLibraryViewModel>> Handle(
        GetPlexLibrariesBySourceId request,
        CancellationToken cancellationToken)
    {
        await using TvContext context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await context.PlexLibraries
            .AsNoTracking()
            .Where(l => l.MediaSourceId == request.PlexMediaSourceId)
            .Map(pl => ProjectToViewModel(pl))
            .ToListAsync(cancellationToken);
    }
}
