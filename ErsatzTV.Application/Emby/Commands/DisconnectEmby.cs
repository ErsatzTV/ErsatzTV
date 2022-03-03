using ErsatzTV.Core;
using LanguageExt;

namespace ErsatzTV.Application.Emby;

public record DisconnectEmby : MediatR.IRequest<Either<BaseError, Unit>>;