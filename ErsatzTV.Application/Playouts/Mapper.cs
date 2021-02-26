using System.IO;
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
            new(GetDisplayTitle(playoutItem.MediaItem), playoutItem.Start, GetDisplayDuration(playoutItem.MediaItem));

        private static PlayoutChannelViewModel Project(Channel channel) =>
            new(channel.Id, channel.Number, channel.Name);

        private static PlayoutProgramScheduleViewModel Project(ProgramSchedule programSchedule) =>
            new(programSchedule.Id, programSchedule.Name);

        private static string GetDisplayTitle(MediaItem mediaItem) =>
            mediaItem switch
            {
                Episode e => e.EpisodeMetadata.HeadOrNone()
                    .Map(em => $"{em.Title} - s{e.Season.SeasonNumber:00}e{e.EpisodeNumber:00}")
                    .IfNone(Path.GetFileName(e.Path)),
                Movie m => m.MovieMetadata.HeadOrNone().Map(mm => mm.Title).IfNone(Path.GetFileName(m.Path)),
                _ => string.Empty
            };

        private static string GetDisplayDuration(MediaItem mediaItem) =>
            string.Format(
                mediaItem.Statistics.Duration.TotalHours >= 1 ? @"{0:h\:mm\:ss}" : @"{0:mm\:ss}",
                mediaItem.Statistics.Duration);
    }
}
