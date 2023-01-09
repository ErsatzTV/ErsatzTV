using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.Playouts;

internal static class Mapper
{
    internal static PlayoutItemViewModel ProjectToViewModel(PlayoutItem playoutItem) =>
        new(
            GetDisplayTitle(playoutItem),
            playoutItem.StartOffset,
            GetDisplayDuration(playoutItem.FinishOffset - playoutItem.StartOffset));

    internal static PlayoutAlternateScheduleViewModel ProjectToViewModel(
        ProgramScheduleAlternate programScheduleAlternate) =>
        new(
            programScheduleAlternate.Id,
            programScheduleAlternate.Index,
            programScheduleAlternate.ProgramScheduleId,
            programScheduleAlternate.DaysOfWeek,
            programScheduleAlternate.DaysOfMonth,
            programScheduleAlternate.MonthsOfYear);

    private static string GetDisplayTitle(PlayoutItem playoutItem)
    {
        switch (playoutItem.MediaItem)
        {
            case Episode e:
                string showTitle = e.Season.Show.ShowMetadata.HeadOrNone()
                    .Map(sm => $"{sm.Title} - ").IfNone(string.Empty);
                var episodeNumbers = e.EpisodeMetadata.Map(em => em.EpisodeNumber).ToList();
                var episodeTitles = e.EpisodeMetadata.Map(em => em.Title).ToList();
                if (episodeNumbers.Count == 0 || episodeTitles.Count == 0)
                {
                    return "[unknown episode]";
                }

                var numbersString = $"e{string.Join('e', episodeNumbers.Map(n => $"{n:00}"))}";
                var titlesString = $"{string.Join('/', episodeTitles)}";
                if (!string.IsNullOrWhiteSpace(playoutItem.ChapterTitle))
                {
                    titlesString += $" ({playoutItem.ChapterTitle})";
                }

                return $"{showTitle}s{e.Season.SeasonNumber:00}{numbersString} - {titlesString}";
            case Movie m:
                return m.MovieMetadata.HeadOrNone().Map(mm => mm.Title).IfNone("[unknown movie]");
            case MusicVideo mv:
                string artistName = mv.Artist.ArtistMetadata.HeadOrNone()
                    .Map(am => $"{am.Title} - ").IfNone(string.Empty);
                return mv.MusicVideoMetadata.HeadOrNone()
                    .Map(mvm => $"{artistName}{mvm.Title}")
                    .Map(
                        s => string.IsNullOrWhiteSpace(playoutItem.ChapterTitle)
                            ? s
                            : $"{s} ({playoutItem.ChapterTitle})")
                    .IfNone("[unknown music video]");
            case OtherVideo ov:
                return ov.OtherVideoMetadata.HeadOrNone()
                    .Map(ovm => ovm.Title ?? string.Empty)
                    .Map(
                        s => string.IsNullOrWhiteSpace(playoutItem.ChapterTitle)
                            ? s
                            : $"{s} ({playoutItem.ChapterTitle})")
                    .IfNone("[unknown video]");
            case Song s:
                string songArtist = s.SongMetadata.HeadOrNone()
                    .Map(sm => string.IsNullOrWhiteSpace(sm.Artist) ? string.Empty : $"{sm.Artist} - ")
                    .IfNone(string.Empty);
                return s.SongMetadata.HeadOrNone()
                    .Map(sm => $"{songArtist}{sm.Title ?? string.Empty}")
                    .Map(
                        t => string.IsNullOrWhiteSpace(playoutItem.ChapterTitle)
                            ? t
                            : $"{s} ({playoutItem.ChapterTitle})")
                    .IfNone("[unknown song]");
            default:
                return string.Empty;
        }
    }

    private static string GetDisplayDuration(TimeSpan duration) =>
        string.Format(
            duration.TotalHours >= 1 ? @"{0:h\:mm\:ss}" : @"{0:mm\:ss}",
            duration);
}
