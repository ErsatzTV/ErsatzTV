using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core.Interfaces.Repositories;
using MediatR;
using static ErsatzTV.Application.ProgramSchedules.Mapper;

namespace ErsatzTV.Application.ProgramSchedules.Queries
{
    public class GetAllProgramSchedulesHandler : IRequestHandler<GetAllProgramSchedules, List<ProgramScheduleViewModel>>
    {
        private readonly IProgramScheduleRepository _programScheduleRepository;

        public GetAllProgramSchedulesHandler(IProgramScheduleRepository programScheduleRepository) =>
            _programScheduleRepository = programScheduleRepository;

        public async Task<List<ProgramScheduleViewModel>> Handle(
            GetAllProgramSchedules request,
            CancellationToken cancellationToken) =>
            (await _programScheduleRepository.GetAll()).Map(ProjectToViewModel).ToList();
    }
}
