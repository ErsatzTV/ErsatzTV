using ErsatzTV.Core;
using LanguageExt;

namespace ErsatzTV.Application.MediaItems.Commands
{
    public record RefreshMediaItem(int MediaItemId) : MediatR.IRequest<Either<BaseError, Unit>>,
        IBackgroundServiceRequest;
}
