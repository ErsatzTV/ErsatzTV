using ErsatzTV.Core;

namespace ErsatzTV.Application.Channels;

public record DeleteChannel(int ChannelId) : IRequest<Either<BaseError, Unit>>;
