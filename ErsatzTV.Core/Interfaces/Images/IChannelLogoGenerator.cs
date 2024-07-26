using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Interfaces.Images;

public interface IChannelLogoGenerator
{
    Either<BaseError, byte[]> GenerateChannelLogo(
        string text,
        int logoHeight,
        int logoWidth,
        CancellationToken cancellationToken);
}
