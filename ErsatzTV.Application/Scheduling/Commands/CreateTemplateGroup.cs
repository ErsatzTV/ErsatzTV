using ErsatzTV.Core;

namespace ErsatzTV.Application.Scheduling;

public record CreateTemplateGroup(string Name) : IRequest<Either<BaseError, TemplateGroupViewModel>>;
