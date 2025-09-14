using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Interfaces.FFmpeg;

public interface IDecoSelector
{
    DecoEntries GetDecoEntries(Playout playout, DateTimeOffset now);
}
