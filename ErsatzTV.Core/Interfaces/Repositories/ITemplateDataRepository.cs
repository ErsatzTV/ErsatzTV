using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Interfaces.Repositories;

public interface ITemplateDataRepository
{
    Task<Option<Dictionary<string, object>>> GetMediaItemTemplateData(MediaItem mediaItem);

    Task<Option<Dictionary<string, object>>> GetEpgTemplateData(
        string channelNumber,
        DateTimeOffset time,
        int count);
}
