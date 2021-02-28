using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.Images.Commands
{
    // ReSharper disable once SuggestBaseTypeForParameter
    public record SaveArtworkToDisk(byte[] Buffer, ArtworkKind ArtworkKind) : IRequest<Either<BaseError, string>>;
}
