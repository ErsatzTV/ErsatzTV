using ErsatzTV.Core;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.Images.Commands
{
    // ReSharper disable once SuggestBaseTypeForParameter
    public record SaveImageToDisk(byte[] Buffer) : IRequest<Either<BaseError, string>>;
}
