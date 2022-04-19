using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Interfaces.Repositories;

public interface IFFmpegProfileRepository
{
    Task<FFmpegProfile> Copy(int ffmpegProfileId, string name);
}
