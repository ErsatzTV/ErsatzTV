using ErsatzTV.Application.MediaItems;
using ErsatzTV.Core.Domain;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using static ErsatzTV.Application.MediaItems.Mapper;

namespace ErsatzTV.Application.Artists;

public class GetAllArtistsHandler : IRequestHandler<GetAllArtists, List<NamedMediaItemViewModel>>
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;

    public GetAllArtistsHandler(IDbContextFactory<TvContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<List<NamedMediaItemViewModel>> Handle(
        GetAllArtists request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        List<Artist> allArtists = await dbContext.Artists
            .AsNoTracking()
            .Include(a => a.ArtistMetadata)
            .ToListAsync(cancellationToken: cancellationToken);
            
        return allArtists.Bind(a => ProjectArtist(a)).ToList();
    }

    private static Option<NamedMediaItemViewModel> ProjectArtist(Artist a)
    {
        foreach (ArtistMetadata metadata in a.ArtistMetadata.HeadOrNone())
        {
            if (!string.IsNullOrWhiteSpace(metadata.Title))
            {
                return ProjectToViewModel(a);
            }
        }

        return None;
    }
}
