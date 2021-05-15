using System.Collections.Generic;
using ErsatzTV.Core;
using LanguageExt;

namespace ErsatzTV.Application.Jellyfin.Commands
{
    public record UpdateJellyfinPathReplacements(
        int JellyfinMediaSourceId,
        List<JellyfinPathReplacementItem> PathReplacements) : MediatR.IRequest<Either<BaseError, Unit>>;

    public record JellyfinPathReplacementItem(int Id, string JellyfinPath, string LocalPath);
}
