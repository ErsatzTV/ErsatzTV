using ErsatzTV.Core;

namespace ErsatzTV.Application.Scheduling;

public record CopyTemplate(int TemplateId, int NewTemplateGroupId, string NewTemplateName)
    : IRequest<Either<BaseError, TemplateViewModel>>;
