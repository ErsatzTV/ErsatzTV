using ErsatzTV.Core;

namespace ErsatzTV.Application.MediaItems;

public record GetMediaItemInfo(int Id) : IRequest<Either<BaseError, MediaItemInfo>>;
