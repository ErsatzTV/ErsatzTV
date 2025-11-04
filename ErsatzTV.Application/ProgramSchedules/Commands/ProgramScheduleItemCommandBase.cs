using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Filler;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.ProgramSchedules;

public abstract class ProgramScheduleItemCommandBase
{
    protected static Task<Validation<BaseError, ProgramSchedule>> ProgramScheduleMustExist(
        TvContext dbContext,
        int programScheduleId,
        CancellationToken cancellationToken) =>
        dbContext.ProgramSchedules
            .Include(ps => ps.Items)
            .Include(ps => ps.Playouts)
            .SelectOneAsync(ps => ps.Id, ps => ps.Id == programScheduleId, cancellationToken)
            .Map(o => o.ToValidation<BaseError>("[ProgramScheduleId] does not exist."));

    protected static async Task<Either<BaseError, ProgramSchedule>> FillerConfigurationMustBeValid(
        TvContext dbContext,
        IProgramScheduleItemRequest item,
        ProgramSchedule programSchedule)
    {
        var allFillerIds = Optional(item.PreRollFillerId)
            .Append(Optional(item.MidRollFillerId))
            .Append(Optional(item.PostRollFillerId))
            .ToList();

        List<FillerPreset> allFiller = await dbContext.FillerPresets
            .Filter(fp => allFillerIds.Contains(fp.Id))
            .ToListAsync();

        if (allFiller.Count(f => f.PadToNearestMinute.HasValue) > 1)
        {
            return BaseError.New("Schedule may only contain one filler preset that is configured to pad");
        }

        if (allFiller.Any(fp => fp.PadToNearestMinute.HasValue) && !item.FallbackFillerId.HasValue)
        {
            return BaseError.New("Fallback filler is required when padding");
        }

        return programSchedule;
    }

    protected static Validation<BaseError, ProgramSchedule> PlayoutModeMustBeValid(
        IProgramScheduleItemRequest item,
        ProgramSchedule programSchedule)
    {
        if (item.MultiCollectionId.HasValue)
        {
            switch (item.PlaybackOrder)
            {
                case PlaybackOrder.Chronological:
                case PlaybackOrder.Random:
                case PlaybackOrder.MultiEpisodeShuffle:
                case PlaybackOrder.SeasonEpisode:
                case PlaybackOrder.RandomRotation:
                    return BaseError.New($"Invalid playback order for multi collection: '{item.PlaybackOrder}'");
                case PlaybackOrder.Shuffle:
                case PlaybackOrder.ShuffleInOrder:
                    break;
            }
        }

        switch (item.PlayoutMode)
        {
            case PlayoutMode.Flood:
            case PlayoutMode.One:
                break;
            case PlayoutMode.Multiple:
                if (item.MultipleMode is MultipleMode.PlaylistItemSize &&
                    item.CollectionType is not CollectionType.Playlist)
                {
                    return BaseError.New(
                        "[MultipleMode] cannot be [PlaylistItemSize] when collection is not a playlist");
                }

                if (item.MultipleMode is MultipleMode.Count && item.MultipleCount.GetValueOrDefault() < 1)
                {
                    return BaseError.New(
                        "[MultipleCount] must be greater than 0 for playout mode 'multiple / count'");
                }

                break;
            case PlayoutMode.Duration:
                if (item.PlayoutDuration is null)
                {
                    return BaseError.New("[PlayoutDuration] is required for playout mode 'duration'");
                }

                if (item.PlayoutDuration <= TimeSpan.Zero || item.PlayoutDuration > TimeSpan.FromHours(24))
                {
                    return BaseError.New("[PlayoutDuration] must be between 1 minute and 24 hours");
                }

                if (item.DiscardToFillAttempts is null)
                {
                    return BaseError.New("[DiscardToFillAttempts] is required for playout mode 'duration'");
                }

                if (item.TailMode == TailMode.Filler && item.TailFillerId == null)
                {
                    return BaseError.New("Tail Filler is required with tail mode Filler");
                }

                if (item.TailFillerId != null && item.TailMode != TailMode.Filler)
                {
                    return BaseError.New("Tail Filler will not be used unless tail mode is set to Filler");
                }

                break;
            default:
                return BaseError.New("[PlayoutMode] is invalid");
        }

        return programSchedule;
    }

    protected static Validation<BaseError, ProgramSchedule> CollectionTypeMustBeValid(
        IProgramScheduleItemRequest item,
        ProgramSchedule programSchedule)
    {
        switch (item.CollectionType)
        {
            case CollectionType.Collection:
                if (item.CollectionId is null)
                {
                    return BaseError.New("[Collection] is required for collection type 'Collection'");
                }

                break;
            case CollectionType.TelevisionShow:
                if (item.MediaItemId is null)
                {
                    return BaseError.New("[MediaItem] is required for collection type 'TelevisionShow'");
                }

                break;
            case CollectionType.TelevisionSeason:
                if (item.MediaItemId is null)
                {
                    return BaseError.New("[MediaItem] is required for collection type 'TelevisionSeason'");
                }

                break;
            case CollectionType.Artist:
                if (item.MediaItemId is null)
                {
                    return BaseError.New("[MediaItem] is required for collection type 'Artist'");
                }

                break;
            case CollectionType.MultiCollection:
                if (item.MultiCollectionId is null)
                {
                    return BaseError.New("[MultiCollection] is required for collection type 'MultiCollection'");
                }

                break;
            case CollectionType.SmartCollection:
                if (item.SmartCollectionId is null)
                {
                    return BaseError.New("[SmartCollection] is required for collection type 'SmartCollection'");
                }

                break;
            case CollectionType.RerunFirstRun:
                if (item.RerunCollectionId is null)
                {
                    return BaseError.New("[RerunCollection] is required for collection type 'RerunFirstRun'");
                }

                break;
            case CollectionType.RerunRerun:
                if (item.RerunCollectionId is null)
                {
                    return BaseError.New("[RerunCollection] is required for collection type 'RerunRerun'");
                }

                break;
            case CollectionType.Playlist:
                if (item.PlaylistId is null)
                {
                    return BaseError.New("[Playlist] is required for collection type 'Playlist'");
                }

                break;
            case CollectionType.SearchQuery:
                if (string.IsNullOrWhiteSpace(item.SearchQuery))
                {
                    return BaseError.New("[SearchQuery] is required for collection type 'SearchQuery'");
                }

                break;
            default:
                return BaseError.New("[CollectionType] is invalid");
        }

        return programSchedule;
    }

    protected static ProgramScheduleItem BuildItem(
        ProgramSchedule programSchedule,
        int index,
        IProgramScheduleItemRequest item)
    {
        ProgramScheduleItem result = item.PlayoutMode switch
        {
            PlayoutMode.Flood => new ProgramScheduleItemFlood
            {
                ProgramScheduleId = programSchedule.Id,
                Index = index,
                StartTime = FixStartTime(item.StartTime),
                FixedStartTimeBehavior = item.FixedStartTimeBehavior,
                CollectionType = item.CollectionType,
                CollectionId = item.CollectionId,
                MultiCollectionId = item.MultiCollectionId,
                SmartCollectionId = item.SmartCollectionId,
                RerunCollectionId = item.RerunCollectionId,
                MediaItemId = item.MediaItemId,
                PlaylistId = item.PlaylistId,
                SearchTitle = item.SearchTitle,
                SearchQuery = item.SearchQuery,
                PlaybackOrder = item.PlaybackOrder,
                MarathonGroupBy = item.MarathonGroupBy,
                MarathonShuffleGroups = item.MarathonShuffleGroups,
                MarathonShuffleItems = item.MarathonShuffleItems,
                MarathonBatchSize = item.MarathonBatchSize,
                FillWithGroupMode = FillWithGroupMode.None,
                CustomTitle = item.CustomTitle,
                GuideMode = item.GuideMode,
                PreRollFillerId = item.PreRollFillerId,
                MidRollFillerId = item.MidRollFillerId,
                PostRollFillerId = item.PostRollFillerId,
                TailFillerId = item.TailFillerId,
                FallbackFillerId = item.FallbackFillerId,
                PreferredAudioLanguageCode = item.PreferredAudioLanguageCode,
                PreferredAudioTitle = item.PreferredAudioTitle,
                PreferredSubtitleLanguageCode = item.PreferredSubtitleLanguageCode,
                SubtitleMode = item.SubtitleMode
            },
            PlayoutMode.One => new ProgramScheduleItemOne
            {
                ProgramScheduleId = programSchedule.Id,
                Index = index,
                StartTime = FixStartTime(item.StartTime),
                FixedStartTimeBehavior = item.FixedStartTimeBehavior,
                CollectionType = item.CollectionType,
                CollectionId = item.CollectionId,
                MultiCollectionId = item.MultiCollectionId,
                SmartCollectionId = item.SmartCollectionId,
                RerunCollectionId = item.RerunCollectionId,
                MediaItemId = item.MediaItemId,
                PlaylistId = item.PlaylistId,
                SearchTitle = item.SearchTitle,
                SearchQuery = item.SearchQuery,
                PlaybackOrder = item.PlaybackOrder,
                MarathonGroupBy = item.MarathonGroupBy,
                MarathonShuffleGroups = item.MarathonShuffleGroups,
                MarathonShuffleItems = item.MarathonShuffleItems,
                MarathonBatchSize = item.MarathonBatchSize,
                FillWithGroupMode = FillWithGroupMode.None,
                CustomTitle = item.CustomTitle,
                GuideMode = item.GuideMode,
                PreRollFillerId = item.PreRollFillerId,
                MidRollFillerId = item.MidRollFillerId,
                PostRollFillerId = item.PostRollFillerId,
                TailFillerId = item.TailFillerId,
                FallbackFillerId = item.FallbackFillerId,
                PreferredAudioLanguageCode = item.PreferredAudioLanguageCode,
                PreferredAudioTitle = item.PreferredAudioTitle,
                PreferredSubtitleLanguageCode = item.PreferredSubtitleLanguageCode,
                SubtitleMode = item.SubtitleMode
            },
            PlayoutMode.Multiple => new ProgramScheduleItemMultiple
            {
                ProgramScheduleId = programSchedule.Id,
                Index = index,
                StartTime = FixStartTime(item.StartTime),
                FixedStartTimeBehavior = item.FixedStartTimeBehavior,
                CollectionType = item.CollectionType,
                CollectionId = item.CollectionId,
                MultiCollectionId = item.MultiCollectionId,
                SmartCollectionId = item.SmartCollectionId,
                RerunCollectionId = item.RerunCollectionId,
                MediaItemId = item.MediaItemId,
                PlaylistId = item.PlaylistId,
                SearchTitle = item.SearchTitle,
                SearchQuery = item.SearchQuery,
                PlaybackOrder = item.PlaybackOrder,
                MarathonGroupBy = item.MarathonGroupBy,
                MarathonShuffleGroups = item.MarathonShuffleGroups,
                MarathonShuffleItems = item.MarathonShuffleItems,
                MarathonBatchSize = item.MarathonBatchSize,
                FillWithGroupMode = item.FillWithGroupMode,
                MultipleMode = item.MultipleMode,
                Count = item.MultipleMode is MultipleMode.Count ? item.MultipleCount.GetValueOrDefault() : 0,
                CustomTitle = item.CustomTitle,
                GuideMode = item.GuideMode,
                PreRollFillerId = item.PreRollFillerId,
                MidRollFillerId = item.MidRollFillerId,
                PostRollFillerId = item.PostRollFillerId,
                TailFillerId = item.TailFillerId,
                FallbackFillerId = item.FallbackFillerId,
                PreferredAudioLanguageCode = item.PreferredAudioLanguageCode,
                PreferredAudioTitle = item.PreferredAudioTitle,
                PreferredSubtitleLanguageCode = item.PreferredSubtitleLanguageCode,
                SubtitleMode = item.SubtitleMode
            },
            PlayoutMode.Duration => new ProgramScheduleItemDuration
            {
                ProgramScheduleId = programSchedule.Id,
                Index = index,
                StartTime = FixStartTime(item.StartTime),
                FixedStartTimeBehavior = item.FixedStartTimeBehavior,
                CollectionType = item.CollectionType,
                CollectionId = item.CollectionId,
                MultiCollectionId = item.MultiCollectionId,
                SmartCollectionId = item.SmartCollectionId,
                RerunCollectionId = item.RerunCollectionId,
                MediaItemId = item.MediaItemId,
                PlaylistId = item.PlaylistId,
                SearchTitle = item.SearchTitle,
                SearchQuery = item.SearchQuery,
                PlaybackOrder = item.PlaybackOrder,
                MarathonGroupBy = item.MarathonGroupBy,
                MarathonShuffleGroups = item.MarathonShuffleGroups,
                MarathonShuffleItems = item.MarathonShuffleItems,
                MarathonBatchSize = item.MarathonBatchSize,
                FillWithGroupMode = item.FillWithGroupMode,
                PlayoutDuration = item.PlayoutDuration.GetValueOrDefault(),
                TailMode = item.TailMode,
                DiscardToFillAttempts = FixDiscardToFillAttempts(
                    item.PlaybackOrder,
                    item.DiscardToFillAttempts.GetValueOrDefault()),
                CustomTitle = item.CustomTitle,
                GuideMode = item.GuideMode,
                PreRollFillerId = item.PreRollFillerId,
                MidRollFillerId = item.MidRollFillerId,
                PostRollFillerId = item.PostRollFillerId,
                TailFillerId = item.TailFillerId,
                FallbackFillerId = item.FallbackFillerId,
                PreferredAudioLanguageCode = item.PreferredAudioLanguageCode,
                PreferredAudioTitle = item.PreferredAudioTitle,
                PreferredSubtitleLanguageCode = item.PreferredSubtitleLanguageCode,
                SubtitleMode = item.SubtitleMode
            },
            _ => throw new NotSupportedException($"Unsupported playout mode {item.PlayoutMode}")
        };

        foreach (int watermarkId in item.WatermarkIds)
        {
            result.ProgramScheduleItemWatermarks ??= [];
            result.ProgramScheduleItemWatermarks.Add(
                new ProgramScheduleItemWatermark
                {
                    ProgramScheduleItem = result,
                    WatermarkId = watermarkId
                });
        }

        foreach (int graphicsElementId in item.GraphicsElementIds)
        {
            result.ProgramScheduleItemGraphicsElements ??= [];
            result.ProgramScheduleItemGraphicsElements.Add(
                new ProgramScheduleItemGraphicsElement
                {
                    ProgramScheduleItem = result,
                    GraphicsElementId = graphicsElementId
                });
        }

        return result;
    }

    private static TimeSpan? FixStartTime(TimeSpan? startTime) =>
        startTime.HasValue && startTime.Value >= TimeSpan.FromDays(1)
            ? startTime.Value.Subtract(TimeSpan.FromDays(1))
            : startTime;

    private static int FixDiscardToFillAttempts(PlaybackOrder playbackOrder, int value) => playbackOrder switch
    {
        PlaybackOrder.Random or PlaybackOrder.Shuffle => value,
        _ => 0
    };
}
