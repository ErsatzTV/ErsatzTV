using ErsatzTV.Core;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.Streaming;

public record GetHlsPlaylistByChannelNumber
    (string Scheme, string Host, string ChannelNumber, string Mode) : IRequest<Either<BaseError, string>>;