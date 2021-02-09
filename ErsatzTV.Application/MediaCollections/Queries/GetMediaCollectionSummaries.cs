using System.Collections.Generic;
using MediatR;

namespace ErsatzTV.Application.MediaCollections.Queries
{
    public record GetMediaCollectionSummaries(string SearchString) : IRequest<List<MediaCollectionSummaryViewModel>>;
}
