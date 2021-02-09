using System.Threading.Tasks;
using ErsatzTV.Core;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.MediaItems.Commands
{
    public record DeleteMediaItem(int MediaItemId) : IRequest<Either<BaseError, Task>>;
}
