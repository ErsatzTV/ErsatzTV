using ErsatzTV.Core;

namespace ErsatzTV.Application.Scheduling;

public record CreateTemplate(int TemplateGroupId, string Name) : IRequest<Either<BaseError, TemplateViewModel>>;
