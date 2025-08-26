using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Interfaces.Repositories;

public interface ITemplateDataRepository
{
    Task<Option<Dictionary<string, object>>> GetMediaItemTemplateData(
        MediaItem mediaItem,
        CancellationToken cancellationToken);

    Task<Option<Dictionary<string, object>>> GetEpgTemplateData(
        string channelNumber,
        DateTimeOffset time,
        int count);
}
