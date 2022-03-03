using System.Collections.Generic;
using MediatR;

namespace ErsatzTV.Application.Jellyfin;

public record GetAllJellyfinMediaSources : IRequest<List<JellyfinMediaSourceViewModel>>;