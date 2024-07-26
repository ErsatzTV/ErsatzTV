using ErsatzTV.Core;

namespace ErsatzTV.Application.Playouts;

public record UpdateTemplatePlayout(int PlayoutId, string TemplateFile)
    : IRequest<Either<BaseError, PlayoutNameViewModel>>;
