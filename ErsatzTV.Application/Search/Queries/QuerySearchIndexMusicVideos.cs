using ErsatzTV.Application.MediaCards;

namespace ErsatzTV.Application.Search;

public record QuerySearchIndexMusicVideos
    (string Query, int PageNumber, int PageSize) : IRequest<MusicVideoCardResultsViewModel>;
