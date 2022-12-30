using ErsatzTV.Core;

namespace ErsatzTV.Scanner.Application.MediaSources;

public record ScanLocalLibrary(int LibraryId, bool ForceScan) : IRequest<Either<BaseError, string>>;
