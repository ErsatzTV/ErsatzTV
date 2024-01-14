using ErsatzTV.Core;

namespace ErsatzTV.Application.Scheduling;

public record DeleteTemplate(int TemplateId) : IRequest<Option<BaseError>>;
