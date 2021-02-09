using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using MediatR;
using static ErsatzTV.Application.ProgramSchedules.Mapper;

namespace ErsatzTV.Application.ProgramSchedules.Queries
{
    public class GetProgramScheduleItemsHandler : IRequestHandler<GetProgramScheduleItems,
        Option<IEnumerable<ProgramScheduleItemViewModel>>>
    {
        private readonly IProgramScheduleRepository _programScheduleRepository;

        public GetProgramScheduleItemsHandler(IProgramScheduleRepository programScheduleRepository) =>
            _programScheduleRepository = programScheduleRepository;

        public Task<Option<IEnumerable<ProgramScheduleItemViewModel>>> Handle(
            GetProgramScheduleItems request,
            CancellationToken cancellationToken) =>
            _programScheduleRepository.GetItems(request.Id)
                .MapT(programScheduleItems => programScheduleItems.Map(ProjectToViewModel));
    }
}
