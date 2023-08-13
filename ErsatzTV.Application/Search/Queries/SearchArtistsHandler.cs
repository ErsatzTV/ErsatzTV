using Dapper;
using ErsatzTV.Application.MediaItems;
using ErsatzTV.Core.Domain;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Search;

public class SearchArtistsHandler : IRequestHandler<SearchArtists, List<NamedMediaItemViewModel>>
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;

    public SearchArtistsHandler(IDbContextFactory<TvContext> dbContextFactory) =>
        _dbContextFactory = dbContextFactory;

    public async Task<List<NamedMediaItemViewModel>> Handle(SearchArtists request, CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.ArtistMetadata
            .AsNoTracking()
            .Where(
                a => EF.Functions.Like(
                    EF.Functions.Collate(a.Title, TvContext.CaseInsensitiveCollation),
                    $"%{request.Query}%"))
            .OrderBy(a => EF.Functions.Collate(a.Title, TvContext.CaseInsensitiveCollation))
            .Take(10)
            .ToListAsync(cancellationToken)
            .Map(list => list.Bind(a => ToNamedMediaItem(a)).ToList());
    }

    private static Option<NamedMediaItemViewModel> ToNamedMediaItem(ArtistMetadata artist)
    {
        if (string.IsNullOrWhiteSpace(artist.Title))
        {
            return Option<NamedMediaItemViewModel>.None;
        }

        return new NamedMediaItemViewModel(artist.Id, artist.Title);
    }
}
