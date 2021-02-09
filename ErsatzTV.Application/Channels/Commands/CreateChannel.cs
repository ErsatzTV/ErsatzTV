using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.Channels.Commands
{
    public record CreateChannel
    (
        string Name,
        int Number,
        int FFmpegProfileId,
        string Logo,
        StreamingMode StreamingMode) : IRequest<Either<BaseError, ChannelViewModel>>;
}
