using ErsatzTV.Core;

namespace ErsatzTV.Application.Scheduling;

public record CreateDecoTemplate(int DecoTemplateGroupId, string Name) : IRequest<Either<BaseError, DecoTemplateViewModel>>;
