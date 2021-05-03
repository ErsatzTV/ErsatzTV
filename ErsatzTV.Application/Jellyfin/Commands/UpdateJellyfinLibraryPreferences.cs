using System.Collections.Generic;
using ErsatzTV.Core;
using LanguageExt;

namespace ErsatzTV.Application.Jellyfin.Commands
{
    public record UpdateJellyfinLibraryPreferences
        (List<JellyfinLibraryPreference> Preferences) : MediatR.IRequest<Either<BaseError, Unit>>;

    public record JellyfinLibraryPreference(int Id, bool ShouldSyncItems);
}
