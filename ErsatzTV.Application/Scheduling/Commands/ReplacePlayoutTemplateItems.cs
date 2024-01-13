using ErsatzTV.Core;

namespace ErsatzTV.Application.Scheduling;

public record ReplacePlayoutTemplateItems(int PlayoutId, List<ReplacePlayoutTemplate> Items)
    : IRequest<Option<BaseError>>;
