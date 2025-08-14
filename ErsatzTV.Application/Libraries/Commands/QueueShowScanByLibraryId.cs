namespace ErsatzTV.Application.Libraries;

public record QueueShowScanByLibraryId(int LibraryId, int ShowId, string ShowTitle, bool DeepScan) : IRequest<bool>;