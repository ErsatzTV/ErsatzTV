using ErsatzTV.Core;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.Streaming.Queries
{
    public record GetHlsPlaylistByChannelNumber
        (string Scheme, string Host, int ChannelNumber) : IRequest<Either<BaseError, string>>;
}
