using ErsatzTV.Core.Domain;
using static LanguageExt.Prelude;

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
            mediaItem.Metadata.MediaType == MediaType.TvShow &&
            Optional(mediaItem.Metadata.SeasonNumber).IsSome &&
            Optional(mediaItem.Metadata.EpisodeNumber).IsSome
                ? $"{mediaItem.Metadata.Title} s{mediaItem.Metadata.SeasonNumber:00}e{mediaItem.Metadata.EpisodeNumber:00}"
                : mediaItem.Metadata.Title;

        public static string GetDisplayDuration(MediaItem mediaItem) =>
            string.Format(
                mediaItem.Metadata.Duration.TotalHours >= 1 ? @"{0:h\:mm\:ss}" : @"{0:mm\:ss}",
                mediaItem.Metadata.Duration);
    }
}
