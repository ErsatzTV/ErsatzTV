using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using MediatR;
using static ErsatzTV.Application.ProgramSchedules.Mapper;

namespace ErsatzTV.Application.ProgramSchedules.Queries
{
    public class
        GetProgramScheduleByIdHandler : IRequestHandler<GetProgramScheduleById, Option<ProgramScheduleViewModel>>
    {
        private readonly IProgramScheduleRepository _programScheduleRepository;

        public GetProgramScheduleByIdHandler(IProgramScheduleRepository programScheduleRepository) =>
            _programScheduleRepository = programScheduleRepository;

        public Task<Option<ProgramScheduleViewModel>> Handle(
            GetProgramScheduleById request,
            CancellationToken cancellationToken) =>
            _programScheduleRepository.Get(request.Id)
                .MapT(ProjectToViewModel);
    }
}
