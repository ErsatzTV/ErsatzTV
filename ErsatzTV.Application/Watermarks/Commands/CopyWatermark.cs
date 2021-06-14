using ErsatzTV.Core;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.Watermarks.Commands
{
    public record CopyWatermark
        (int WatermarkId, string Name) : IRequest<Either<BaseError, WatermarkViewModel>>;
}
