﻿using ErsatzTV.Core;

namespace ErsatzTV.Application.Plex;

public record UpdatePlexPathReplacements
(
    int PlexMediaSourceId,
    List<PlexPathReplacementItem> PathReplacements) : MediatR.IRequest<Either<BaseError, Unit>>;

public record PlexPathReplacementItem(int Id, string PlexPath, string LocalPath);