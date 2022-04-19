using ErsatzTV.Core;

namespace ErsatzTV.Application.Plex;

public record UpdatePlexLibraryPreferences
    (List<PlexLibraryPreference> Preferences) : IRequest<Either<BaseError, Unit>>;

public record PlexLibraryPreference(int Id, bool ShouldSyncItems);
