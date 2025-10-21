using ErsatzTV.Core;

namespace ErsatzTV.Scanner.Application.Emby;

public record SynchronizeEmbyShowById(string BaseUrl, int EmbyLibraryId, int ShowId, bool DeepScan)
    : IRequest<Either<BaseError, string>>;
