﻿using ErsatzTV.Core;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.Playouts.Commands
{
    public record DeletePlayout(int PlayoutId) : IRequest<Option<BaseError>>;
}
