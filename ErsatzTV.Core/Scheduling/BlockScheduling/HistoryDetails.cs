using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Scheduling;
using Newtonsoft.Json;

namespace ErsatzTV.Core.Scheduling.BlockScheduling;

internal static class HistoryDetails
{
    private static readonly JsonSerializerSettings JsonSettings = new()
    {
        NullValueHandling = NullValueHandling.Ignore
    };
    
    public static string KeyForBlockItem(BlockItem blockItem)
    {
        dynamic key = new
        {
            blockItem.BlockId,
            blockItem.PlaybackOrder,
            blockItem.CollectionType,
            blockItem.CollectionId,
            blockItem.MultiCollectionId,
            blockItem.SmartCollectionId,
            blockItem.MediaItemId
        };

        return JsonConvert.SerializeObject(key, Formatting.None, JsonSettings);
    }

    public static string ForMediaItem(MediaItem mediaItem)
    {
        Details details = mediaItem switch
        {
            Episode e => ForEpisode(e),
            _ => new Details(mediaItem.Id, null, null, null)
        };
            
        return JsonConvert.SerializeObject(details, Formatting.None, JsonSettings);
    }

    private static Details ForEpisode(Episode e)
    {
        int? episodeNumber = null;
        DateTime? releaseDate = null;
        foreach (EpisodeMetadata episodeMetadata in e.EpisodeMetadata.HeadOrNone())
        {
            episodeNumber = episodeMetadata.EpisodeNumber;
            releaseDate = episodeMetadata.ReleaseDate;
        }

        return new Details(e.Id, releaseDate, e.Season.SeasonNumber, episodeNumber);
    }

    public record Details(int? MediaItemId, DateTime? ReleaseDate, int? SeasonNumber, int? EpisodeNumber);
}
