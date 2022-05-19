using ErsatzTV.Application.MediaCards;

namespace ErsatzTV.Application.Search;

public record QuerySearchIndexOtherVideos
    (string Query, int PageNumber, int PageSize) : IRequest<OtherVideoCardResultsViewModel>;
