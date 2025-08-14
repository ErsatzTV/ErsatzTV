using ErsatzTV.Core;

namespace ErsatzTV.Application.Scheduling;

public record ReplaceDecoTemplateItems(
    int DecoTemplateId,
    int DecoTemplateGroupId,
    string Name,
    List<ReplaceDecoTemplateItem> Items)
    : IRequest<Either<BaseError, List<DecoTemplateItemViewModel>>>;
