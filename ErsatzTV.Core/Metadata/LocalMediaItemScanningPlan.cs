using System.Collections.Generic;
using ErsatzTV.Core.Domain;
using LanguageExt;

namespace ErsatzTV.Core.Metadata
{
    public record LocalMediaItemScanningPlan(Either<string, MediaItem> Source, List<ItemScanningPlan> ActionPlans);
}
