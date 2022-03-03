using System.Collections.Generic;
using MediatR;

namespace ErsatzTV.Application.Plex;

public record GetPlexLibrariesBySourceId(int PlexMediaSourceId) : IRequest<List<PlexLibraryViewModel>>;