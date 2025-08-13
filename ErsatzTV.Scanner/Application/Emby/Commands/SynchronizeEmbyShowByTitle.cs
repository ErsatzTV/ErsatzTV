using ErsatzTV.Core;

namespace ErsatzTV.Scanner.Application.Emby;

public record SynchronizeEmbyShowByTitle(int EmbyLibraryId, string ShowTitle, bool DeepScan)
    : IRequest<Either<BaseError, string>>;