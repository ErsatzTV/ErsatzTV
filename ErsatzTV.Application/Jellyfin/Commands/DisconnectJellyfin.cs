using ErsatzTV.Core;
using LanguageExt;

namespace ErsatzTV.Application.Jellyfin;

public record DisconnectJellyfin : MediatR.IRequest<Either<BaseError, Unit>>;