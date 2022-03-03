using ErsatzTV.Core;
using LanguageExt;
using MediatR;
using Unit = LanguageExt.Unit;

namespace ErsatzTV.Application.Playouts;

public record DeletePlayout(int PlayoutId) : IRequest<Either<BaseError, Unit>>;