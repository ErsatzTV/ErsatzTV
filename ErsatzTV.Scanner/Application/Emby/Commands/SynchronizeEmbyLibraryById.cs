using ErsatzTV.Core;

namespace ErsatzTV.Scanner.Application.Emby;

public record SynchronizeEmbyLibraryById
    (int EmbyLibraryId, bool ForceScan, bool DeepScan) : IRequest<Either<BaseError, string>>;
