using ErsatzTV.Core;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.Jellyfin.Commands
{
    public interface ISynchronizeJellyfinLibraryById : IRequest<Either<BaseError, string>>,
        IJellyfinBackgroundServiceRequest
    {
        int JellyfinLibraryId { get; }
        bool ForceScan { get; }
    }

    public record SynchronizeJellyfinLibraryByIdIfNeeded(int JellyfinLibraryId) : ISynchronizeJellyfinLibraryById
    {
        public bool ForceScan => false;
    }

    public record ForceSynchronizeJellyfinLibraryById(int JellyfinLibraryId) : ISynchronizeJellyfinLibraryById
    {
        public bool ForceScan => true;
    }
}
