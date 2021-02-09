using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using MediatR;
using static ErsatzTV.Application.ProgramSchedules.Mapper;

namespace ErsatzTV.Application.ProgramSchedules.Commands
{
    public class
        CreateProgramScheduleHandler : IRequestHandler<CreateProgramSchedule,
            Either<BaseError, ProgramScheduleViewModel>>
    {
        private readonly IProgramScheduleRepository _programScheduleRepository;

        public CreateProgramScheduleHandler(IProgramScheduleRepository programScheduleRepository) =>
            _programScheduleRepository = programScheduleRepository;

        public Task<Either<BaseError, ProgramScheduleViewModel>> Handle(
            CreateProgramSchedule request,
            CancellationToken cancellationToken) =>
            Validate(request)
                .Map(PersistProgramSchedule)
                .ToEitherAsync();

        private Task<ProgramScheduleViewModel> PersistProgramSchedule(ProgramSchedule c) =>
            _programScheduleRepository.Add(c).Map(ProjectToViewModel);

        private Validation<BaseError, ProgramSchedule> Validate(CreateProgramSchedule request) =>
            ValidateName(request)
                .Map(
                    name => new ProgramSchedule
                    {
                        Name = name, MediaCollectionPlaybackOrder = request.MediaCollectionPlaybackOrder
                    });

        private Validation<BaseError, string> ValidateName(CreateProgramSchedule createProgramSchedule) =>
            createProgramSchedule.NotEmpty(c => c.Name)
                .Bind(_ => createProgramSchedule.NotLongerThan(50)(c => c.Name));
    }
}
