using ErsatzTV.Core;

namespace ErsatzTV.Application.Playouts;

public record UpdateYamlPlayout(int PlayoutId, string TemplateFile)
    : IRequest<Either<BaseError, PlayoutNameViewModel>>;
