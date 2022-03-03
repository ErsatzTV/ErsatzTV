using ErsatzTV.Application.MediaCards;

namespace ErsatzTV.Application.Search;

public record QuerySearchIndexSeasons
    (string Query, int PageNumber, int PageSize) : IRequest<TelevisionSeasonCardResultsViewModel>;