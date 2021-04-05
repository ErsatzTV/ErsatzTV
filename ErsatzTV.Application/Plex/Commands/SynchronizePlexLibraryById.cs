using ErsatzTV.Core;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.Plex.Commands
{
    public interface ISynchronizePlexLibraryById : IRequest<Either<BaseError, string>>, IPlexBackgroundServiceRequest
    {
        int PlexLibraryId { get; }
        bool ForceScan { get; }
    }

    public record SynchronizePlexLibraryByIdIfNeeded
        (int PlexLibraryId) : ISynchronizePlexLibraryById
    {
        public bool ForceScan => false;
    }

    public record ForceSynchronizePlexLibraryById
        (int PlexLibraryId) : ISynchronizePlexLibraryById
    {
        public bool ForceScan => true;
    }
}
