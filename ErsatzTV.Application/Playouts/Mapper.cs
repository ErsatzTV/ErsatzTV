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
                TelevisionEpisodeMediaItem e => e.Metadata != null
                    ? $"{e.Metadata.Title} - s{e.Metadata.Season:00}e{e.Metadata.Episode:00}"
                    : Path.GetFileName(e.Path),
                Movie m => m.Metadata?.Title ?? Path.GetFileName(m.Path),
                _ => string.Empty
            };

        private static string GetDisplayDuration(MediaItem mediaItem) =>
            string.Format(
                mediaItem.Statistics.Duration.TotalHours >= 1 ? @"{0:h\:mm\:ss}" : @"{0:mm\:ss}",
                mediaItem.Statistics.Duration);
    }
}
