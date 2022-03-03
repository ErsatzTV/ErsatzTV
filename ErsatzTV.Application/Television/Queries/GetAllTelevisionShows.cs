using System.Collections.Generic;
using ErsatzTV.Application.MediaItems;
using MediatR;

namespace ErsatzTV.Application.Television;

public record GetAllTelevisionShows : IRequest<List<NamedMediaItemViewModel>>;