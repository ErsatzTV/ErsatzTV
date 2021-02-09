using System.Threading.Tasks;
using ErsatzTV.Core;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.Channels.Commands
{
    public record DeleteChannel(int ChannelId) : IRequest<Either<BaseError, Task>>;
}
