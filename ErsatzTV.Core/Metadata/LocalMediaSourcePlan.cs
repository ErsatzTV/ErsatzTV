using System.Collections.Generic;
using ErsatzTV.Core.Domain;
using LanguageExt;

namespace ErsatzTV.Core.Metadata
{
    public record LocalMediaSourcePlan(Either<string, MediaItem> Source, List<ActionPlan> ActionPlans)
    {
        public Either<string, MediaItem> Source { get; set; } = Source;
    }
}
