using ErsatzTV.Core;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.Plex.Commands
{
    public interface ISynchronizePlexLibraryById : IRequest<Either<BaseError, string>>, IBackgroundServiceRequest
    {
        int PlexMediaSourceId { get; }
        int PlexLibraryId { get; }
        bool ForceScan { get; }
    }

    public record SynchronizePlexLibraryByIdIfNeeded
        (int PlexMediaSourceId, int PlexLibraryId) : ISynchronizePlexLibraryById
    {
        public bool ForceScan => false;
    }

    public record ForceSynchronizePlexLibraryById
        (int PlexMediaSourceId, int PlexLibraryId) : ISynchronizePlexLibraryById
    {
        public bool ForceScan => true;
    }
}
