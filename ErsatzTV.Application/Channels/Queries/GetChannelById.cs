using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.Channels;

public record GetChannelById(int Id) : IRequest<Option<ChannelViewModel>>;