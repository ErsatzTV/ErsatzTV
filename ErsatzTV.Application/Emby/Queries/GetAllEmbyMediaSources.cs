using System.Collections.Generic;
using MediatR;

namespace ErsatzTV.Application.Emby;

public record GetAllEmbyMediaSources : IRequest<List<EmbyMediaSourceViewModel>>;