using MediatR;

namespace ErsatzTV.Core.MediaSources;

public record ScannerProgressUpdate(
    int LibraryId,
    int[] ItemsToReindex,
    int[] ItemsToRemove) : INotification;
