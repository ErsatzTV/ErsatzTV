using ErsatzTV.Core;

namespace ErsatzTV.Application.Scheduling;

public record DeleteDecoTemplateGroup(int DecoTemplateGroupId) : IRequest<Option<BaseError>>;
