using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.ProgramSchedules.Commands
{
    public class DeleteProgramScheduleHandler : IRequestHandler<DeleteProgramSchedule, Either<BaseError, Task>>
    {
        private readonly IProgramScheduleRepository _programScheduleRepository;

        public DeleteProgramScheduleHandler(IProgramScheduleRepository programScheduleRepository) =>
            _programScheduleRepository = programScheduleRepository;

        public async Task<Either<BaseError, Task>> Handle(
            DeleteProgramSchedule request,
            CancellationToken cancellationToken) =>
            (await ProgramScheduleMustExist(request))
            .Map(DoDeletion)
            .ToEither<Task>();

        private Task DoDeletion(int programScheduleId) => _programScheduleRepository.Delete(programScheduleId);

        private async Task<Validation<BaseError, int>> ProgramScheduleMustExist(
            DeleteProgramSchedule deleteProgramSchedule) =>
            (await _programScheduleRepository.Get(deleteProgramSchedule.ProgramScheduleId))
            .ToValidation<BaseError>($"ProgramSchedule {deleteProgramSchedule.ProgramScheduleId} does not exist.")
            .Map(c => c.Id);
    }
}
