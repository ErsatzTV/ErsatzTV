using System.Threading.Tasks;
using ErsatzTV.Core;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.MediaCollections.Commands
{
    public record DeleteSimpleMediaCollection(int SimpleMediaCollectionId) : IRequest<Either<BaseError, Task>>;
}
