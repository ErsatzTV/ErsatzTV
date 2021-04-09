using System;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.Playouts
{
    internal static class Mapper
    {
        internal static PlayoutViewModel ProjectToViewModel(Playout playout) =>
            new(
                playout.Id,
                Project(playout.Channel),
                Project(playout.ProgramSchedule),
                playout.ProgramSchedulePlayoutType);

        internal static PlayoutItemViewModel ProjectToViewModel(PlayoutItem playoutItem) =>
            new(
                GetDisplayTitle(playoutItem.MediaItem),
                playoutItem.StartOffset,
                GetDisplayDuration(playoutItem.MediaItem));

        private static PlayoutChannelViewModel Project(Channel channel) =>
            new(channel.Id, channel.Number, channel.Name);

        private static PlayoutProgramScheduleViewModel Project(ProgramSchedule programSchedule) =>
            new(programSchedule.Id, programSchedule.Name);

        private static string GetDisplayTitle(MediaItem mediaItem)
        {
            switch (mediaItem)
            {
                case Episode e:
                    string showTitle = e.Season.Show.ShowMetadata.HeadOrNone()
                        .Map(sm => $"{sm.Title} - ").IfNone(string.Empty);
                    return e.EpisodeMetadata.HeadOrNone()
                        .Map(em => $"{showTitle}s{e.Season.SeasonNumber:00}e{e.EpisodeNumber:00} - {em.Title}")
                        .IfNone("[unknown episode]");
                case Movie m:
                    return m.MovieMetadata.HeadOrNone().Map(mm => mm.Title).IfNone("[unknown movie]");
                case MusicVideo mv:
                    string artistName = mv.Artist.ArtistMetadata.HeadOrNone()
                        .Map(am => $"{am.Title} - ").IfNone(string.Empty);
                    return mv.MusicVideoMetadata.HeadOrNone()
                        .Map(mvm => $"{artistName}{mvm.Title}")
                        .IfNone("[unknown music video]");
                default:
                    return string.Empty;
            }
        }

        private static string GetDisplayDuration(MediaItem mediaItem)
        {
            MediaVersion version = mediaItem switch
            {
                Movie m => m.MediaVersions.Head(),
                Episode e => e.MediaVersions.Head(),
                MusicVideo mv => mv.MediaVersions.Head(),
                _ => throw new ArgumentOutOfRangeException(nameof(mediaItem))
            };

            return string.Format(
                version.Duration.TotalHours >= 1 ? @"{0:h\:mm\:ss}" : @"{0:mm\:ss}",
                version.Duration);
        }
    }
}
