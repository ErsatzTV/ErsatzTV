using System.Collections.Generic;
using MediatR;

namespace ErsatzTV.Application.MediaCollections.Queries
{
    public record GetAllSimpleMediaCollections : IRequest<List<MediaCollectionViewModel>>;
}
