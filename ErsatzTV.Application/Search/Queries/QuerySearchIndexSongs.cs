using ErsatzTV.Application.MediaCards;

namespace ErsatzTV.Application.Search;

public record QuerySearchIndexSongs
    (string Query, int PageNumber, int PageSize) : IRequest<SongCardResultsViewModel>;
