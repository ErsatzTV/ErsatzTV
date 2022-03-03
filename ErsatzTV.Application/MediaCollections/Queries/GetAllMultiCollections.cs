using System.Collections.Generic;
using MediatR;

namespace ErsatzTV.Application.MediaCollections;

public record GetAllMultiCollections : IRequest<List<MultiCollectionViewModel>>;