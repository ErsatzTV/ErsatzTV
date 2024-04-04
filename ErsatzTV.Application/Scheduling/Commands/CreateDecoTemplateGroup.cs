using ErsatzTV.Core;

namespace ErsatzTV.Application.Scheduling;

public record CreateDecoTemplateGroup(string Name) : IRequest<Either<BaseError, DecoTemplateGroupViewModel>>;
