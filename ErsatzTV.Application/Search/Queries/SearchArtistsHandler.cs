using ErsatzTV.Application.MediaItems;
using ErsatzTV.Core.Domain;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Search;

public class SearchArtistsHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<SearchArtists, List<NamedMediaItemViewModel>>
{
    public async Task<List<NamedMediaItemViewModel>> Handle(SearchArtists request, CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.ArtistMetadata
            .AsNoTracking()
            .Where(a => EF.Functions.Like(a.Title, $"%{request.Query}%"))
            .OrderBy(a => a.Title)
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

        return new NamedMediaItemViewModel(artist.ArtistId, artist.Title);
    }
}
