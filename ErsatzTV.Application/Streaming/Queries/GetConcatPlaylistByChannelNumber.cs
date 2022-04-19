using ErsatzTV.Core;
using ErsatzTV.Core.FFmpeg;

namespace ErsatzTV.Application.Streaming;

public record GetConcatPlaylistByChannelNumber
    (string Scheme, string Host, string ChannelNumber) : IRequest<Either<BaseError, ConcatPlaylist>>;
