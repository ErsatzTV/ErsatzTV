using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.ProgramSchedules;

internal static class Mapper
{
    internal static ProgramScheduleViewModel ProjectToViewModel(ProgramSchedule programSchedule) =>
        new(
            programSchedule.Id,
            programSchedule.Name,
            programSchedule.KeepMultiPartEpisodesTogether,
            programSchedule.TreatCollectionsAsShows,
            programSchedule.ShuffleScheduleItems,
            programSchedule.RandomStartPoint,
            programSchedule.FixedStartTimeBehavior);

    internal static ProgramScheduleItemViewModel ProjectToViewModel(ProgramScheduleItem programScheduleItem) =>
        programScheduleItem switch
        {
            ProgramScheduleItemDuration duration =>
                new ProgramScheduleItemDurationViewModel(
                    duration.Id,
                    duration.Index,
                    duration.StartType,
                    duration.StartTime,
                    duration.FixedStartTimeBehavior,
                    duration.CollectionType,
                    duration.Collection != null
                        ? MediaCollections.Mapper.ProjectToViewModel(duration.Collection)
                        : null,
                    duration.MultiCollection != null
                        ? MediaCollections.Mapper.ProjectToViewModel(duration.MultiCollection)
                        : null,
                    duration.SmartCollection != null
                        ? MediaCollections.Mapper.ProjectToViewModel(duration.SmartCollection)
                        : null,
                    duration.RerunCollection != null
                        ? MediaCollections.Mapper.ProjectToViewModel(duration.RerunCollection)
                        : null,
                    duration.Playlist != null
                        ? MediaCollections.Mapper.ProjectToViewModel(duration.Playlist)
                        : null,
                    duration.MediaItem switch
                    {
                        Show show => MediaItems.Mapper.ProjectToViewModel(show),
                        Season season => MediaItems.Mapper.ProjectToViewModel(season),
                        Artist artist => MediaItems.Mapper.ProjectToViewModel(artist),
                        _ => null
                    },
                    duration.SearchTitle,
                    duration.SearchQuery,
                    duration.PlaybackOrder,
                    duration.MarathonGroupBy,
                    duration.MarathonShuffleGroups,
                    duration.MarathonShuffleItems,
                    duration.MarathonBatchSize,
                    duration.FillWithGroupMode,
                    duration.PlayoutDuration,
                    duration.TailMode,
                    duration.DiscardToFillAttempts,
                    duration.CustomTitle,
                    duration.GuideMode,
                    duration.PreRollFiller != null
                        ? Filler.Mapper.ProjectToViewModel(duration.PreRollFiller)
                        : null,
                    duration.MidRollFiller != null
                        ? Filler.Mapper.ProjectToViewModel(duration.MidRollFiller)
                        : null,
                    duration.PostRollFiller != null
                        ? Filler.Mapper.ProjectToViewModel(duration.PostRollFiller)
                        : null,
                    duration.TailFiller != null
                        ? Filler.Mapper.ProjectToViewModel(duration.TailFiller)
                        : null,
                    duration.FallbackFiller != null
                        ? Filler.Mapper.ProjectToViewModel(duration.FallbackFiller)
                        : null,
                    duration.ProgramScheduleItemWatermarks.Map(wm => Watermarks.Mapper.ProjectToViewModel(wm.Watermark))
                        .ToList(),
                    duration.ProgramScheduleItemGraphicsElements.Map(ge => Graphics.Mapper.ProjectToViewModel(ge.GraphicsElement))
                        .ToList(),
                    duration.PreferredAudioLanguageCode,
                    duration.PreferredAudioTitle,
                    duration.PreferredSubtitleLanguageCode,
                    duration.SubtitleMode),
            ProgramScheduleItemFlood flood =>
                new ProgramScheduleItemFloodViewModel(
                    flood.Id,
                    flood.Index,
                    flood.StartType,
                    flood.StartTime,
                    flood.FixedStartTimeBehavior,
                    flood.CollectionType,
                    flood.Collection != null
                        ? MediaCollections.Mapper.ProjectToViewModel(flood.Collection)
                        : null,
                    flood.MultiCollection != null
                        ? MediaCollections.Mapper.ProjectToViewModel(flood.MultiCollection)
                        : null,
                    flood.SmartCollection != null
                        ? MediaCollections.Mapper.ProjectToViewModel(flood.SmartCollection)
                        : null,
                    flood.RerunCollection != null
                        ? MediaCollections.Mapper.ProjectToViewModel(flood.RerunCollection)
                        : null,
                    flood.Playlist != null
                        ? MediaCollections.Mapper.ProjectToViewModel(flood.Playlist)
                        : null,
                    flood.MediaItem switch
                    {
                        Show show => MediaItems.Mapper.ProjectToViewModel(show),
                        Season season => MediaItems.Mapper.ProjectToViewModel(season),
                        Artist artist => MediaItems.Mapper.ProjectToViewModel(artist),
                        _ => null
                    },
                    flood.SearchTitle,
                    flood.SearchQuery,
                    flood.PlaybackOrder,
                    flood.MarathonGroupBy,
                    flood.MarathonShuffleGroups,
                    flood.MarathonShuffleItems,
                    flood.MarathonBatchSize,
                    flood.FillWithGroupMode,
                    flood.CustomTitle,
                    flood.GuideMode,
                    flood.PreRollFiller != null
                        ? Filler.Mapper.ProjectToViewModel(flood.PreRollFiller)
                        : null,
                    flood.MidRollFiller != null
                        ? Filler.Mapper.ProjectToViewModel(flood.MidRollFiller)
                        : null,
                    flood.PostRollFiller != null
                        ? Filler.Mapper.ProjectToViewModel(flood.PostRollFiller)
                        : null,
                    flood.TailFiller != null
                        ? Filler.Mapper.ProjectToViewModel(flood.TailFiller)
                        : null,
                    flood.FallbackFiller != null
                        ? Filler.Mapper.ProjectToViewModel(flood.FallbackFiller)
                        : null,
                    flood.ProgramScheduleItemWatermarks.Map(wm => Watermarks.Mapper.ProjectToViewModel(wm.Watermark))
                        .ToList(),
                    flood.ProgramScheduleItemGraphicsElements.Map(ge => Graphics.Mapper.ProjectToViewModel(ge.GraphicsElement))
                        .ToList(),
                    flood.PreferredAudioLanguageCode,
                    flood.PreferredAudioTitle,
                    flood.PreferredSubtitleLanguageCode,
                    flood.SubtitleMode),
            ProgramScheduleItemMultiple multiple =>
                new ProgramScheduleItemMultipleViewModel(
                    multiple.Id,
                    multiple.Index,
                    multiple.StartType,
                    multiple.StartTime,
                    multiple.FixedStartTimeBehavior,
                    multiple.CollectionType,
                    multiple.Collection != null
                        ? MediaCollections.Mapper.ProjectToViewModel(multiple.Collection)
                        : null,
                    multiple.MultiCollection != null
                        ? MediaCollections.Mapper.ProjectToViewModel(multiple.MultiCollection)
                        : null,
                    multiple.SmartCollection != null
                        ? MediaCollections.Mapper.ProjectToViewModel(multiple.SmartCollection)
                        : null,
                    multiple.RerunCollection != null
                        ? MediaCollections.Mapper.ProjectToViewModel(multiple.RerunCollection)
                        : null,
                    multiple.Playlist != null
                        ? MediaCollections.Mapper.ProjectToViewModel(multiple.Playlist)
                        : null,
                    multiple.MediaItem switch
                    {
                        Show show => MediaItems.Mapper.ProjectToViewModel(show),
                        Season season => MediaItems.Mapper.ProjectToViewModel(season),
                        Artist artist => MediaItems.Mapper.ProjectToViewModel(artist),
                        _ => null
                    },
                    multiple.SearchTitle,
                    multiple.SearchQuery,
                    multiple.PlaybackOrder,
                    multiple.MarathonGroupBy,
                    multiple.MarathonShuffleGroups,
                    multiple.MarathonShuffleItems,
                    multiple.MarathonBatchSize,
                    multiple.FillWithGroupMode,
                    multiple.MultipleMode,
                    multiple.Count,
                    multiple.CustomTitle,
                    multiple.GuideMode,
                    multiple.PreRollFiller != null
                        ? Filler.Mapper.ProjectToViewModel(multiple.PreRollFiller)
                        : null,
                    multiple.MidRollFiller != null
                        ? Filler.Mapper.ProjectToViewModel(multiple.MidRollFiller)
                        : null,
                    multiple.PostRollFiller != null
                        ? Filler.Mapper.ProjectToViewModel(multiple.PostRollFiller)
                        : null,
                    multiple.TailFiller != null
                        ? Filler.Mapper.ProjectToViewModel(multiple.TailFiller)
                        : null,
                    multiple.FallbackFiller != null
                        ? Filler.Mapper.ProjectToViewModel(multiple.FallbackFiller)
                        : null,
                    multiple.ProgramScheduleItemWatermarks.Map(wm => Watermarks.Mapper.ProjectToViewModel(wm.Watermark))
                        .ToList(),
                    multiple.ProgramScheduleItemGraphicsElements.Map(ge => Graphics.Mapper.ProjectToViewModel(ge.GraphicsElement))
                        .ToList(),
                    multiple.PreferredAudioLanguageCode,
                    multiple.PreferredAudioTitle,
                    multiple.PreferredSubtitleLanguageCode,
                    multiple.SubtitleMode),
            ProgramScheduleItemOne one =>
                new ProgramScheduleItemOneViewModel(
                    one.Id,
                    one.Index,
                    one.StartType,
                    one.StartTime,
                    one.FixedStartTimeBehavior,
                    one.CollectionType,
                    one.Collection != null
                        ? MediaCollections.Mapper.ProjectToViewModel(one.Collection)
                        : null,
                    one.MultiCollection != null
                        ? MediaCollections.Mapper.ProjectToViewModel(one.MultiCollection)
                        : null,
                    one.SmartCollection != null
                        ? MediaCollections.Mapper.ProjectToViewModel(one.SmartCollection)
                        : null,
                    one.RerunCollection != null
                        ? MediaCollections.Mapper.ProjectToViewModel(one.RerunCollection)
                        : null,
                    one.Playlist != null
                        ? MediaCollections.Mapper.ProjectToViewModel(one.Playlist)
                        : null,
                    one.MediaItem switch
                    {
                        Show show => MediaItems.Mapper.ProjectToViewModel(show),
                        Season season => MediaItems.Mapper.ProjectToViewModel(season),
                        Artist artist => MediaItems.Mapper.ProjectToViewModel(artist),
                        _ => null
                    },
                    one.SearchTitle,
                    one.SearchQuery,
                    one.PlaybackOrder,
                    one.MarathonGroupBy,
                    one.MarathonShuffleGroups,
                    one.MarathonShuffleItems,
                    one.MarathonBatchSize,
                    one.FillWithGroupMode,
                    one.CustomTitle,
                    one.GuideMode,
                    one.PreRollFiller != null
                        ? Filler.Mapper.ProjectToViewModel(one.PreRollFiller)
                        : null,
                    one.MidRollFiller != null
                        ? Filler.Mapper.ProjectToViewModel(one.MidRollFiller)
                        : null,
                    one.PostRollFiller != null
                        ? Filler.Mapper.ProjectToViewModel(one.PostRollFiller)
                        : null,
                    one.TailFiller != null
                        ? Filler.Mapper.ProjectToViewModel(one.TailFiller)
                        : null,
                    one.FallbackFiller != null
                        ? Filler.Mapper.ProjectToViewModel(one.FallbackFiller)
                        : null,
                    one.ProgramScheduleItemWatermarks.Map(wm => Watermarks.Mapper.ProjectToViewModel(wm.Watermark))
                        .ToList(),
                    one.ProgramScheduleItemGraphicsElements.Map(ge => Graphics.Mapper.ProjectToViewModel(ge.GraphicsElement))
                        .ToList(),
                    one.PreferredAudioLanguageCode,
                    one.PreferredAudioTitle,
                    one.PreferredSubtitleLanguageCode,
                    one.SubtitleMode),
            _ => throw new NotSupportedException(
                $"Unsupported program schedule item type {programScheduleItem.GetType().Name}")
        };
}
