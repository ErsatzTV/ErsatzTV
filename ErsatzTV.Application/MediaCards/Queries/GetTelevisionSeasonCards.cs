﻿using MediatR;

namespace ErsatzTV.Application.MediaCards.Queries
{
    public record GetTelevisionSeasonCards
        (int TelevisionShowId, int PageNumber, int PageSize) : IRequest<TelevisionSeasonCardResultsViewModel>;
}
