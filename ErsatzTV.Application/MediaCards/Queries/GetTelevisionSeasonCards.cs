using MediatR;

namespace ErsatzTV.Application.MediaCards;

public record GetTelevisionSeasonCards
    (int TelevisionShowId, int PageNumber, int PageSize) : IRequest<TelevisionSeasonCardResultsViewModel>;