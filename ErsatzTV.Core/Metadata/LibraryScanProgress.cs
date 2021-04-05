using MediatR;

namespace ErsatzTV.Core.Metadata
{
    public record LibraryScanProgress(int LibraryId, decimal Progress) : INotification;
}
