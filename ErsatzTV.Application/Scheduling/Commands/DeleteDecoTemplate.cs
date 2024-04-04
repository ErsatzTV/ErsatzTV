using ErsatzTV.Core;

namespace ErsatzTV.Application.Scheduling;

public record DeleteDecoTemplate(int DecoTemplateId) : IRequest<Option<BaseError>>;
