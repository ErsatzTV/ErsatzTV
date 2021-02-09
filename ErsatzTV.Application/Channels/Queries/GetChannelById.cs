using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.Channels.Queries
{
    public record GetChannelById(int Id) : IRequest<Option<ChannelViewModel>>;
}
