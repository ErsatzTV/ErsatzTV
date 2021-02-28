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
                    .IfNone("[unknown episode]"),
                Movie m => m.MovieMetadata.HeadOrNone().Map(mm => mm.Title).IfNone("[unknown movie]"),
                _ => string.Empty
            };

        private static string GetDisplayDuration(MediaItem mediaItem)
        {
            MediaVersion version = mediaItem switch
            {
                Movie m => m.MediaVersions.Head(),
                Episode e => e.MediaVersions.Head()
            };

            return string.Format(
                version.Duration.TotalHours >= 1 ? @"{0:h\:mm\:ss}" : @"{0:mm\:ss}",
                version.Duration);
        }
    }
}
