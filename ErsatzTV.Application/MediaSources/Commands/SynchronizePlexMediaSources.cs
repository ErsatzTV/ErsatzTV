﻿using System.Collections.Generic;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.MediaSources.Commands
{
    public record
        SynchronizePlexMediaSources : IRequest<Either<BaseError, List<PlexMediaSource>>>, IPlexBackgroundServiceRequest;
}
