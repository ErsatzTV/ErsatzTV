using ErsatzTV.Application.MediaCards;

namespace ErsatzTV.Application.Search;

public record QuerySearchIndexImages(string Query, int PageNumber, int PageSize) : IRequest<ImageCardResultsViewModel>;
