using ErsatzTV.Core;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.Emby;

public interface ISynchronizeEmbyLibraryById : IRequest<Either<BaseError, string>>,
    IEmbyBackgroundServiceRequest
{
    int EmbyLibraryId { get; }
    bool ForceScan { get; }
}

public record SynchronizeEmbyLibraryByIdIfNeeded(int EmbyLibraryId) : ISynchronizeEmbyLibraryById
{
    public bool ForceScan => false;
}

public record ForceSynchronizeEmbyLibraryById(int EmbyLibraryId) : ISynchronizeEmbyLibraryById
{
    public bool ForceScan => true;
}