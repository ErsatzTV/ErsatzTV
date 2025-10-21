using ErsatzTV.Core;

namespace ErsatzTV.Scanner.Application.MediaSources;

public record ScanLocalLibrary(string BaseUrl, int LibraryId, bool ForceScan) : IRequest<Either<BaseError, string>>;
