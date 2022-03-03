using System.Collections.Generic;
using MediatR;

namespace ErsatzTV.Application.Plex;

public record GetAllPlexMediaSources : IRequest<List<PlexMediaSourceViewModel>>;