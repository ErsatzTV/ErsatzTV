using ErsatzTV.Core;

namespace ErsatzTV.Application.Jellyfin;

public record UpdateJellyfinPathReplacements(
    int JellyfinMediaSourceId,
    List<JellyfinPathReplacementItem> PathReplacements) : MediatR.IRequest<Either<BaseError, Unit>>;

public record JellyfinPathReplacementItem(int Id, string JellyfinPath, string LocalPath);