using ErsatzTV.Application.MediaCards;

namespace ErsatzTV.Application.Search;

public record QuerySearchIndexEpisodes
    (string Query, int PageNumber, int PageSize) : IRequest<TelevisionEpisodeCardResultsViewModel>;