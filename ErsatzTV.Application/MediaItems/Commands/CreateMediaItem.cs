using ErsatzTV.Core;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.MediaItems.Commands
{
    public record CreateMediaItem(int MediaSourceId, string Path) : IRequest<Either<BaseError, MediaItemViewModel>>;
}
