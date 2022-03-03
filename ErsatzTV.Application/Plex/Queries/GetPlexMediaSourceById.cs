using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.Plex;

public record GetPlexMediaSourceById(int PlexMediaSourceId) : IRequest<Option<PlexMediaSourceViewModel>>;