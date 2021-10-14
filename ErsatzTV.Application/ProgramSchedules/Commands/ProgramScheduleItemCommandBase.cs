using System;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using LanguageExt;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.ProgramSchedules.Commands
{
    public abstract class ProgramScheduleItemCommandBase
    {
        protected static Task<Validation<BaseError, ProgramSchedule>> ProgramScheduleMustExist(
            TvContext dbContext,
            int programScheduleId) =>
            dbContext.ProgramSchedules
                .Include(ps => ps.Items)
                .Include(ps => ps.Playouts)
                .SelectOneAsync(ps => ps.Id, ps => ps.Id == programScheduleId)
                .Map(o => o.ToValidation<BaseError>("[ProgramScheduleId] does not exist."));

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
                    if (item.MultipleCount.GetValueOrDefault() < 0)
                    {
                        return BaseError.New("[MultipleCount] must be greater than or equal to 0 for playout mode 'multiple'");
                    }

                    break;
                case PlayoutMode.Duration:
                    if (item.PlayoutDuration is null)
                    {
                        return BaseError.New("[PlayoutDuration] is required for playout mode 'duration'");
                    }

                    break;
                default:
                    return BaseError.New("[PlayoutMode] is invalid");
            }

            return programSchedule;
        }

        protected Validation<BaseError, ProgramSchedule> CollectionTypeMustBeValid(
            IProgramScheduleItemRequest item,
            ProgramSchedule programSchedule)
        {
            switch (item.CollectionType)
            {
                case ProgramScheduleItemCollectionType.Collection:
                    if (item.CollectionId is null)
                    {
                        return BaseError.New("[Collection] is required for collection type 'Collection'");
                    }

                    break;
                case ProgramScheduleItemCollectionType.TelevisionShow:
                    if (item.MediaItemId is null)
                    {
                        return BaseError.New("[MediaItem] is required for collection type 'TelevisionShow'");
                    }

                    break;
                case ProgramScheduleItemCollectionType.TelevisionSeason:
                    if (item.MediaItemId is null)
                    {
                        return BaseError.New("[MediaItem] is required for collection type 'TelevisionSeason'");
                    }

                    break;
                case ProgramScheduleItemCollectionType.Artist:
                    if (item.MediaItemId is null)
                    {
                        return BaseError.New("[MediaItem] is required for collection type 'Artist'");
                    }

                    break;
                case ProgramScheduleItemCollectionType.MultiCollection:
                    if (item.MultiCollectionId is null)
                    {
                        return BaseError.New("[MultiCollection] is required for collection type 'MultiCollection'");
                    }

                    break;
                case ProgramScheduleItemCollectionType.SmartCollection:
                    if (item.SmartCollectionId is null)
                    {
                        return BaseError.New("[SmartCollection] is required for collection type 'SmartCollection'");
                    }

                    break;
                default:
                    return BaseError.New("[CollectionType] is invalid");
            }

            return programSchedule;
        }

        protected ProgramScheduleItem BuildItem(
            ProgramSchedule programSchedule,
            int index,
            IProgramScheduleItemRequest item) =>
            item.PlayoutMode switch
            {
                PlayoutMode.Flood => new ProgramScheduleItemFlood
                {
                    ProgramScheduleId = programSchedule.Id,
                    Index = index,
                    StartTime = item.StartTime,
                    CollectionType = item.CollectionType,
                    CollectionId = item.CollectionId,
                    MultiCollectionId = item.MultiCollectionId,
                    SmartCollectionId = item.SmartCollectionId,
                    MediaItemId = item.MediaItemId,
                    PlaybackOrder = item.PlaybackOrder,
                    CustomTitle = item.CustomTitle
                },
                PlayoutMode.One => new ProgramScheduleItemOne
                {
                    ProgramScheduleId = programSchedule.Id,
                    Index = index,
                    StartTime = item.StartTime,
                    CollectionType = item.CollectionType,
                    CollectionId = item.CollectionId,
                    MultiCollectionId = item.MultiCollectionId,
                    SmartCollectionId = item.SmartCollectionId,
                    MediaItemId = item.MediaItemId,
                    PlaybackOrder = item.PlaybackOrder,
                    CustomTitle = item.CustomTitle
                },
                PlayoutMode.Multiple => new ProgramScheduleItemMultiple
                {
                    ProgramScheduleId = programSchedule.Id,
                    Index = index,
                    StartTime = item.StartTime,
                    CollectionType = item.CollectionType,
                    CollectionId = item.CollectionId,
                    MultiCollectionId = item.MultiCollectionId,
                    SmartCollectionId = item.SmartCollectionId,
                    MediaItemId = item.MediaItemId,
                    PlaybackOrder = item.PlaybackOrder,
                    Count = item.MultipleCount.GetValueOrDefault(),
                    CustomTitle = item.CustomTitle
                },
                PlayoutMode.Duration => new ProgramScheduleItemDuration
                {
                    ProgramScheduleId = programSchedule.Id,
                    Index = index,
                    StartTime = item.StartTime,
                    CollectionType = item.CollectionType,
                    CollectionId = item.CollectionId,
                    MultiCollectionId = item.MultiCollectionId,
                    SmartCollectionId = item.SmartCollectionId,
                    MediaItemId = item.MediaItemId,
                    PlaybackOrder = item.PlaybackOrder,
                    PlayoutDuration = FixDuration(item.PlayoutDuration.GetValueOrDefault()),
                    TailMode = item.TailMode,
                    TailCollectionType = item.TailCollectionType,
                    TailCollectionId = item.TailCollectionId,
                    TailMultiCollectionId = item.TailMultiCollectionId,
                    TailSmartCollectionId = item.TailSmartCollectionId,
                    TailMediaItemId = item.TailMediaItemId,
                    CustomTitle = item.CustomTitle
                },
                _ => throw new NotSupportedException($"Unsupported playout mode {item.PlayoutMode}")
            };

        private static TimeSpan FixDuration(TimeSpan duration) =>
            duration > TimeSpan.FromDays(1) ? duration.Subtract(TimeSpan.FromDays(1)) : duration;
    }
}
