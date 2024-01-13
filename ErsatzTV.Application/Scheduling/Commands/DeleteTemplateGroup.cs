using ErsatzTV.Core;

namespace ErsatzTV.Application.Scheduling;

public record DeleteTemplateGroup(int TemplateGroupId) : IRequest<Option<BaseError>>;
