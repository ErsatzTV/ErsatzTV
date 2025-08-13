namespace ErsatzTV.Application.Libraries;

public record QueueShowScanByLibraryId(int LibraryId, string ShowTitle, bool DeepScan) : IRequest<bool>;