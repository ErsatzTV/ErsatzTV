using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Scheduling;

internal class SeasonEpisodeMediaComparer : IComparer<MediaItem>
{
    public int Compare(MediaItem x, MediaItem y)
    {
        if (x == null || y == null)
        {
            return 0;
        }

        int season1 = x switch
        {
            Episode e => e.Season?.SeasonNumber ?? int.MaxValue,
            _ => int.MaxValue
        };

        int season2 = y switch
        {
            Episode e => e.Season?.SeasonNumber ?? int.MaxValue,
            _ => int.MaxValue
        };

        if (season1 != season2)
        {
            return season1.CompareTo(season2);
        }

        int episode1 = x switch
        {
            Episode e => e.EpisodeMetadata.HeadOrNone().Match(
                em => em.EpisodeNumber,
                () => int.MaxValue),
            _ => int.MaxValue
        };

        int episode2 = y switch
        {
            Episode e => e.EpisodeMetadata.HeadOrNone().Match(
                em => em.EpisodeNumber,
                () => int.MaxValue),
            _ => int.MaxValue
        };

        if (episode1 != episode2)
        {
            return episode1.CompareTo(episode2);
        }

        return x.Id.CompareTo(y.Id);
    }
}
