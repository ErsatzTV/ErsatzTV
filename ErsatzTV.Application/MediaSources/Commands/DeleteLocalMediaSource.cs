using System.Threading.Tasks;
using ErsatzTV.Core;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.MediaSources.Commands
{
    public record DeleteLocalMediaSource(int LocalMediaSourceId) : IRequest<Either<BaseError, Task>>;
}
