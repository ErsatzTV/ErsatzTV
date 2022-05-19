using ErsatzTV.Application.MediaCards;

namespace ErsatzTV.Application.Search;

public record QuerySearchIndexMovies
    (string Query, int PageNumber, int PageSize) : IRequest<MovieCardResultsViewModel>;
