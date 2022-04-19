using ErsatzTV.Core;

namespace ErsatzTV.Application.Emby;

public record UpdateEmbyLibraryPreferences
    (List<EmbyLibraryPreference> Preferences) : IRequest<Either<BaseError, Unit>>;

public record EmbyLibraryPreference(int Id, bool ShouldSyncItems);
