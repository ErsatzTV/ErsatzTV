using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;

namespace ErsatzTV.Application.ProgramSchedules.Commands
{
    public abstract class ProgramScheduleItemCommandBase
    {
        private readonly IProgramScheduleRepository _programScheduleRepository;

        protected ProgramScheduleItemCommandBase(IProgramScheduleRepository programScheduleRepository) =>
            _programScheduleRepository = programScheduleRepository;

        protected async Task<Validation<BaseError, ProgramSchedule>> ProgramScheduleMustExist(int programScheduleId) =>
            (await _programScheduleRepository.GetWithPlayouts(programScheduleId))
            .ToValidation<BaseError>("[ProgramScheduleId] does not exist.");

        protected Validation<BaseError, ProgramSchedule> PlayoutModeMustBeValid(
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
                    MediaCollectionId = item.MediaCollectionId
                },
                PlayoutMode.One => new ProgramScheduleItemOne
                {
                    ProgramScheduleId = programSchedule.Id,
                    Index = index,
                    StartTime = item.StartTime,
                    MediaCollectionId = item.MediaCollectionId
                },
                PlayoutMode.Multiple => new ProgramScheduleItemMultiple
                {
                    ProgramScheduleId = programSchedule.Id,
                    Index = index,
                    StartTime = item.StartTime,
                    MediaCollectionId = item.MediaCollectionId,
                    Count = item.MultipleCount.GetValueOrDefault()
                },
                PlayoutMode.Duration => new ProgramScheduleItemDuration
                {
                    ProgramScheduleId = programSchedule.Id,
                    Index = index,
                    StartTime = item.StartTime,
                    MediaCollectionId = item.MediaCollectionId,
                    PlayoutDuration = item.PlayoutDuration.GetValueOrDefault(),
                    OfflineTail = item.OfflineTail.GetValueOrDefault()
                }
            };
    }
}
