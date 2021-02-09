using ErsatzTV.Core;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.Images.Queries
{
    public record GetImageContents(string FileName) : IRequest<Either<BaseError, ImageViewModel>>;
}
