namespace ErsatzTV.Application.Libraries;

public record QueueLibraryScanByLibraryId(int LibraryId) : IRequest<bool>;
