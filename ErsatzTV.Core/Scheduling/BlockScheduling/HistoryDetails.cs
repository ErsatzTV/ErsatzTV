using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Scheduling;
using ErsatzTV.Core.Interfaces.Scheduling;
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
            Movie m => ForMovie(m),
            _ => new Details(mediaItem.Id, null, null, null)
        };
            
        return JsonConvert.SerializeObject(details, Formatting.None, JsonSettings);
    }

    public static void MoveToNextItem(
        List<MediaItem> collectionItems,
        string detailsString,
        IMediaCollectionEnumerator enumerator,
        PlaybackOrder playbackOrder)
    {
        if (playbackOrder is PlaybackOrder.Random)
        {
            return;
        }
        
        Option<MediaItem> maybeMatchedItem = Option<MediaItem>.None;
        var copy = collectionItems.ToList();
        
        Details details = JsonConvert.DeserializeObject<Details>(detailsString);
        if (details.SeasonNumber.HasValue && details.EpisodeNumber.HasValue)
        {
            int season = details.SeasonNumber.Value;
            int episode = details.EpisodeNumber.Value;
            
            maybeMatchedItem = Optional(collectionItems.Find(ci => MatchSeasonAndEpisode(ci, season, episode)));

            if (maybeMatchedItem.IsNone)
            {
                var fakeItem = new Episode
                {
                    Season = new Season { SeasonNumber = season },
                    EpisodeMetadata =
                    [
                        new EpisodeMetadata
                        {
                            EpisodeNumber = episode,
                            ReleaseDate = details.ReleaseDate
                        }
                    ]
                };

                copy.Add(fakeItem);
                maybeMatchedItem = fakeItem;
            }
        }
        else if (playbackOrder is PlaybackOrder.Chronological && details.ReleaseDate.HasValue)
        {
            maybeMatchedItem = Optional(collectionItems.Find(ci => MatchReleaseDate(ci, details.ReleaseDate.Value)));

            if (maybeMatchedItem.IsNone)
            {
                var fakeItem = new Movie { MovieMetadata = [new MovieMetadata { ReleaseDate = details.ReleaseDate }] };
                copy.Add(fakeItem);
                maybeMatchedItem = fakeItem;
            }
        }
        
        foreach (MediaItem matchedItem in maybeMatchedItem)
        {
            IComparer<MediaItem> comparer = playbackOrder switch
            {
                PlaybackOrder.Chronological => new ChronologicalMediaComparer(),
                _ => new SeasonEpisodeMediaComparer(),
            };

            copy.Sort(comparer);

            var state = new CollectionEnumeratorState
            {
                Seed = enumerator.State.Seed,
                Index = copy.IndexOf(matchedItem)
            };
            enumerator.ResetState(state);
            enumerator.MoveNext();
        }
    }

    private static bool MatchReleaseDate(MediaItem mediaItem, DateTime releaseDate) =>
        mediaItem switch
        {
            Movie m => m.MovieMetadata.Any(mm => mm.ReleaseDate == releaseDate),
            Episode e => e.EpisodeMetadata.Any(em => em.ReleaseDate == releaseDate),
            //MusicVideo mv => mv.MusicVideoMetadata.Any(mvm => mvm.ReleaseDate == releaseDate),
            OtherVideo ov => ov.OtherVideoMetadata.Any(ovm => ovm.ReleaseDate == releaseDate),
            _ => false
        };

    private static bool MatchSeasonAndEpisode(MediaItem mediaItem, int seasonNumber, int episodeNumber) =>
        mediaItem switch
        {
            Episode e => e.Season.SeasonNumber == seasonNumber &&
                         e.EpisodeMetadata.Any(em => em.EpisodeNumber == episodeNumber),
            _ => false
        };

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

    private static Details ForMovie(Movie m)
    {
        DateTime? releaseDate = null;
        foreach (MovieMetadata movieMetadata in m.MovieMetadata.HeadOrNone())
        {
            releaseDate = movieMetadata.ReleaseDate;
        }

        return new Details(m.Id, releaseDate, null, null);
    }

    public record Details(int? MediaItemId, DateTime? ReleaseDate, int? SeasonNumber, int? EpisodeNumber);
}
