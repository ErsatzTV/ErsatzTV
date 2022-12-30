using ErsatzTV.Core;

namespace ErsatzTV.Scanner.Application.Emby;

public record SynchronizeEmbyLibraryById(int EmbyLibraryId, bool ForceScan) : IRequest<Either<BaseError, string>>;
