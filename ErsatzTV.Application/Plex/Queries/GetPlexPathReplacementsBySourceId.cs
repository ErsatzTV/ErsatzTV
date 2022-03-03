using System.Collections.Generic;
using MediatR;

namespace ErsatzTV.Application.Plex;

public record GetPlexPathReplacementsBySourceId
    (int PlexMediaSourceId) : IRequest<List<PlexPathReplacementViewModel>>;