using System.Collections.Generic;
using MediatR;

namespace ErsatzTV.Application.Jellyfin;

public record GetJellyfinLibrariesBySourceId(int JellyfinMediaSourceId) : IRequest<List<JellyfinLibraryViewModel>>;