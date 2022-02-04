using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.Channels.Queries;

public record GetChannelFramerate(string ChannelNumber) : IRequest<Option<int>>;
