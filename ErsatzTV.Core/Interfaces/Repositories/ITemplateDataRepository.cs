using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Interfaces.Repositories;

public interface ITemplateDataRepository
{
    public Task<Option<Dictionary<string, object>>> GetMusicVideoTemplateData(
        Resolution resolution,
        TimeSpan streamSeek,
        int musicVideoId);
}