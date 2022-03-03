using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Interfaces.FFmpeg;

public interface IFFmpegLocator
{
    Task<Option<string>> ValidatePath(string executableBase, ConfigElementKey key);
}