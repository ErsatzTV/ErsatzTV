using ErsatzTV.Core;

namespace ErsatzTV.Application.Scheduling;

public record ReplaceTemplateItems(int TemplateId, string Name, List<ReplaceTemplateItem> Items)
    : IRequest<Either<BaseError, List<TemplateItemViewModel>>>;
