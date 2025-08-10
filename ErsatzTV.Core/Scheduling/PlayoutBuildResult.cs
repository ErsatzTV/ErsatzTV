using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Scheduling;

namespace ErsatzTV.Core.Scheduling;

public record PlayoutBuildResult(
    bool ClearItems,
    Option<DateTimeOffset> RemoveBefore,
    Option<DateTimeOffset> RemoveAfter,
    List<PlayoutItem> AddedItems,
    List<int> ItemsToRemove,
    List<PlayoutHistory> AddedHistory,
    List<int> HistoryToRemove,
    Option<DateTimeOffset> TimeShiftTo)
{
    public static PlayoutBuildResult Empty =>
        new(false, Option<DateTimeOffset>.None, Option<DateTimeOffset>.None, [], [], [], [], Option<DateTimeOffset>.None);
}