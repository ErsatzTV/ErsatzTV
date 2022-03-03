using System.Collections.Generic;
using MediatR;

namespace ErsatzTV.Application.MediaCollections;

public record GetAllSmartCollections : IRequest<List<SmartCollectionViewModel>>;