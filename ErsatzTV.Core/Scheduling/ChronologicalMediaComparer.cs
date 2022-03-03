using System;
using System.Collections.Generic;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Scheduling;

internal class ChronologicalMediaComparer : IComparer<MediaItem>
{
    public int Compare(MediaItem x, MediaItem y)
    {
        if (x == null || y == null)
        {
            return 0;
        }

        DateTime date1 = x switch
        {
            Episode e => e.EpisodeMetadata.HeadOrNone().Match(
                em => em.ReleaseDate ?? DateTime.MaxValue,
                () => DateTime.MaxValue),
            Movie m => m.MovieMetadata.HeadOrNone().Match(
                mm => mm.ReleaseDate ?? DateTime.MaxValue,
                () => DateTime.MaxValue),
            MusicVideo mv => mv.MusicVideoMetadata.HeadOrNone().Match(
                mvm => mvm.ReleaseDate ?? DateTime.MaxValue,
                () => DateTime.MaxValue),
            _ => DateTime.MaxValue
        };

        DateTime date2 = y switch
        {
            Episode e => e.EpisodeMetadata.HeadOrNone().Match(
                em => em.ReleaseDate ?? DateTime.MaxValue,
                () => DateTime.MaxValue),
            Movie m => m.MovieMetadata.HeadOrNone().Match(
                mm => mm.ReleaseDate ?? DateTime.MaxValue,
                () => DateTime.MaxValue),
            MusicVideo mv => mv.MusicVideoMetadata.HeadOrNone().Match(
                mvm => mvm.ReleaseDate ?? DateTime.MaxValue,
                () => DateTime.MaxValue),
            _ => DateTime.MaxValue
        };

        if (date1 != date2)
        {
            return date1.CompareTo(date2);
        }

        string songDate1 = x switch
        {
            Song s => s.SongMetadata.HeadOrNone().Match(sm => sm.Date ?? string.Empty, () => string.Empty),
            _ => string.Empty
        };

        string songDate2 = y switch
        {
            Song s => s.SongMetadata.HeadOrNone().Match(sm => sm.Date ?? string.Empty, () => string.Empty),
            _ => string.Empty
        };
            
        if (songDate1 != songDate2)
        {
            return string.Compare(songDate1, songDate2, StringComparison.Ordinal);
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

        string album1 = x switch
        {
            Song s => s.SongMetadata.HeadOrNone().Match(sm => sm.Album ?? string.Empty, () => string.Empty),
            _ => string.Empty
        };

        string album2 = y switch
        {
            Song s => s.SongMetadata.HeadOrNone().Match(sm => sm.Album ?? string.Empty, () => string.Empty),
            _ => string.Empty
        };

        if (album1 != album2)
        {
            return string.Compare(album1, album2, StringComparison.Ordinal);
        }

        string track1 = x switch
        {
            Song s => s.SongMetadata.HeadOrNone().Match(sm => sm.Track ?? string.Empty, () => string.Empty),
            _ => string.Empty
        };

        string track2 = y switch
        {
            Song s => s.SongMetadata.HeadOrNone().Match(sm => sm.Track ?? string.Empty, () => string.Empty),
            _ => string.Empty
        };

        if (track1 != track2)
        {
            return string.Compare(track1, track2, StringComparison.Ordinal);
        }

        return x.Id.CompareTo(y.Id);
    }
}