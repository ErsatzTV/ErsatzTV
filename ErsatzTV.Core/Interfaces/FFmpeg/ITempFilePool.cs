using ErsatzTV.Core.FFmpeg;

namespace ErsatzTV.Core.Interfaces.FFmpeg;

public interface ITempFilePool
{
    string GetNextTempFile(TempFileCategory category);
}