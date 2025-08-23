using ErsatzTV.Core;

namespace ErsatzTV.Application.Playouts;

public record UpdateSequentialPlayout(int PlayoutId, string TemplateFile)
    : IRequest<Either<BaseError, PlayoutNameViewModel>>;
