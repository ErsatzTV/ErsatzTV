using ErsatzTV.Core;
using ErsatzTV.Core.Scheduling;

namespace ErsatzTV.Application.Playouts;

public record BuildPlayout : IRequest<Either<BaseError, Unit>>, IBackgroundServiceRequest
{
    public BuildPlayout(int playoutId, PlayoutBuildMode mode)
    {
        Start = DateTimeOffset.Now;
        PlayoutId = playoutId;
        Mode = mode;
    }

    public BuildPlayout(int playoutId, PlayoutBuildMode mode, DateTimeOffset start)
    {
        Start = start;
        PlayoutId = playoutId;
        Mode = mode;
    }

    public DateTimeOffset Start { get; set; }
    public int PlayoutId { get; init; }
    public PlayoutBuildMode Mode { get; init; }

    public void Deconstruct(out int playoutId, out PlayoutBuildMode mode, out DateTimeOffset start)
    {
        playoutId = PlayoutId;
        mode = Mode;
        start = Start;
    }
}
