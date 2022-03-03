using ErsatzTV.Application.MediaCards;
using MediatR;

namespace ErsatzTV.Application.Search;

public record QuerySearchIndexShows
    (string Query, int PageNumber, int PageSize) : IRequest<TelevisionShowCardResultsViewModel>;