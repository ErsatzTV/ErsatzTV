using System.Collections.Generic;
using MediatR;

namespace ErsatzTV.Application.MediaCollections;

public record GetAllCollections : IRequest<List<MediaCollectionViewModel>>;