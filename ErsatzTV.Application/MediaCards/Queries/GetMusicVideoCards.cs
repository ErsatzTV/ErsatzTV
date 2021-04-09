using MediatR;

namespace ErsatzTV.Application.MediaCards.Queries
{
    public record GetMusicVideoCards
        (int ArtistId, int PageNumber, int PageSize) : IRequest<MusicVideoCardResultsViewModel>;
}
