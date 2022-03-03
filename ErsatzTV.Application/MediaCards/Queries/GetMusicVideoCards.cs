namespace ErsatzTV.Application.MediaCards;

public record GetMusicVideoCards
    (int ArtistId, int PageNumber, int PageSize) : IRequest<MusicVideoCardResultsViewModel>;