using ErsatzTV.Application.MediaCards;
using MediatR;

namespace ErsatzTV.Application.Search.Queries
{
    public record QuerySearchIndexMusicVideos
        (string Query, int PageNumber, int PageSize) : IRequest<MusicVideoCardResultsViewModel>;
}
