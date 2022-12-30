using MediatR;

namespace ErsatzTV.Core.MediaSources;

public record ScannerProgressUpdate(
    int LibraryId,
    string LibraryName,
    decimal? PercentComplete,
    int[] ItemsToReindex,
    int[] ItemsToRemove) : INotification;
