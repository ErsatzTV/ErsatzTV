using ErsatzTV.Application.MediaCards;

namespace ErsatzTV.Application.Search;

public record QuerySearchIndexRemoteStreams(string Query, int PageNumber, int PageSize)
    : IRequest<RemoteStreamCardResultsViewModel>;
