using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Scheduling;

public record PlayoutSchedulerResult(
    PlayoutBuilderState State,
    List<PlayoutItem> PlayoutItems,
    PlayoutBuildWarnings Warnings);
