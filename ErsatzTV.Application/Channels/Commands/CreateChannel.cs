﻿using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.Channels.Commands
{
    public record CreateChannel
    (
        string Name,
        string Number,
        int FFmpegProfileId,
        string Logo,
        StreamingMode StreamingMode) : IRequest<Either<BaseError, ChannelViewModel>>;
}
