﻿using ErsatzTV.Core;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.MediaCollections.Commands
{
    public record AddEpisodeToCollection
        (int CollectionId, int EpisodeId) : IRequest<Either<BaseError, CollectionUpdateResult>>;
}
