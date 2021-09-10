using System.Collections.Generic;
using MediatR;

namespace ErsatzTV.Application.MediaCollections.Queries
{
    public record GetAllSmartCollections : IRequest<List<SmartCollectionViewModel>>;
}
