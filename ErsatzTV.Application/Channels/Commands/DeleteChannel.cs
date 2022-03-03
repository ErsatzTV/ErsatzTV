using System.Threading.Tasks;
using ErsatzTV.Core;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.Channels;

public record DeleteChannel(int ChannelId) : IRequest<Either<BaseError, Task>>;