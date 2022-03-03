namespace ErsatzTV.Application.MediaCards;

public record GetTelevisionEpisodeCards
    (int TelevisionSeasonId, int PageNumber, int PageSize) : IRequest<TelevisionEpisodeCardResultsViewModel>;