using ErsatzTV.Core;
using ErsatzTV.Core.FFmpeg;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.Streaming.Queries
{
    public record GetConcatPlaylistByChannelNumber
        (string Scheme, string Host, string ChannelNumber) : IRequest<Either<BaseError, ConcatPlaylist>>;
}
