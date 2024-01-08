using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Interfaces.Streaming;

public interface IExternalJsonPlayoutItemProvider
{
    Task<Either<BaseError, PlayoutItemWithPath>> CheckForExternalJson(
        Channel channel,
        DateTimeOffset now,
        string ffmpegPath,
        string ffprobePath);
}
