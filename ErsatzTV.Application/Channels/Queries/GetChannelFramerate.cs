using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.Channels;

public record GetChannelFramerate(string ChannelNumber) : IRequest<Option<int>>;
