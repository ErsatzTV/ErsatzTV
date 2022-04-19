using ErsatzTV.Core;

namespace ErsatzTV.Application.Jellyfin;

public record UpdateJellyfinLibraryPreferences
    (List<JellyfinLibraryPreference> Preferences) : IRequest<Either<BaseError, Unit>>;

public record JellyfinLibraryPreference(int Id, bool ShouldSyncItems);
