using System.Collections.Generic;
using ErsatzTV.Core;
using LanguageExt;

namespace ErsatzTV.Application.Emby;

public record UpdateEmbyLibraryPreferences
    (List<EmbyLibraryPreference> Preferences) : MediatR.IRequest<Either<BaseError, Unit>>;

public record EmbyLibraryPreference(int Id, bool ShouldSyncItems);