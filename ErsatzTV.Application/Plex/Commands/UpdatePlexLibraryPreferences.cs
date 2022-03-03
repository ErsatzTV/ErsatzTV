using System.Collections.Generic;
using ErsatzTV.Core;
using LanguageExt;

namespace ErsatzTV.Application.Plex;

public record UpdatePlexLibraryPreferences
    (List<PlexLibraryPreference> Preferences) : MediatR.IRequest<Either<BaseError, Unit>>;

public record PlexLibraryPreference(int Id, bool ShouldSyncItems);