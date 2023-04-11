using Dapper;
using ErsatzTV.Application.MediaItems;
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
        return await dbContext.Connection.QueryAsync<Artist>(
                @"SELECT Artist.Id, AM.Title FROM Artist
                    INNER JOIN ArtistMetadata AM on AM.ArtistId = Artist.Id
                    WHERE AM.Title LIKE @Title
                    ORDER BY AM.Title
                    LIMIT 10
                    COLLATE NOCASE",
                new { Title = $"%{request.Query}%" })
            .Map(list => list.Bind(a => ToNamedMediaItem(a)).ToList());
    }

    private static Option<NamedMediaItemViewModel> ToNamedMediaItem(Artist artist)
    {
        if (string.IsNullOrWhiteSpace(artist.Title))
        {
            return Option<NamedMediaItemViewModel>.None;
        }

        return new NamedMediaItemViewModel(artist.Id, artist.Title);
    }

    public record Artist(int Id, string Title)
    {
        public Artist() : this(default, default)
        {
        }
    }
}
