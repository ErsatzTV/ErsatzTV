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
            switch (item.PlayoutMode)
            {
                case PlayoutMode.Flood:
                case PlayoutMode.One:
                    break;
                case PlayoutMode.Multiple:
                    if (item.MultipleCount.GetValueOrDefault() <= 0)
                    {
                        return BaseError.New("[MultipleCount] must be greater than 0 for playout mode 'multiple'");
                    }

                    break;
                case PlayoutMode.Duration:
                    if (item.PlayoutDuration is null)
                    {
                        return BaseError.New("[PlayoutDuration] is required for playout mode 'duration'");
                    }

                    if (item.OfflineTail is null)
                    {
                        return BaseError.New("[OfflineTail] is required for playout mode 'duration'");
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
                    MediaItemId = item.MediaItemId,
                    CustomTitle = item.CustomTitle
                },
                PlayoutMode.One => new ProgramScheduleItemOne
                {
                    ProgramScheduleId = programSchedule.Id,
                    Index = index,
                    StartTime = item.StartTime,
                    CollectionType = item.CollectionType,
                    CollectionId = item.CollectionId,
                    MediaItemId = item.MediaItemId,
                    CustomTitle = item.CustomTitle
                },
                PlayoutMode.Multiple => new ProgramScheduleItemMultiple
                {
                    ProgramScheduleId = programSchedule.Id,
                    Index = index,
                    StartTime = item.StartTime,
                    CollectionType = item.CollectionType,
                    CollectionId = item.CollectionId,
                    MediaItemId = item.MediaItemId,
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
                    MediaItemId = item.MediaItemId,
                    PlayoutDuration = FixDuration(item.PlayoutDuration.GetValueOrDefault()),
                    OfflineTail = item.OfflineTail.GetValueOrDefault(),
                    CustomTitle = item.CustomTitle
                },
                _ => throw new NotSupportedException($"Unsupported playout mode {item.PlayoutMode}")
            };

        private static TimeSpan FixDuration(TimeSpan duration) =>
            duration > TimeSpan.FromDays(1) ? duration.Subtract(TimeSpan.FromDays(1)) : duration;
    }
}
