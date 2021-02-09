using System.Collections.Generic;
using MediatR;

namespace ErsatzTV.Application.ProgramSchedules.Queries
{
    public record GetAllProgramSchedules : IRequest<List<ProgramScheduleViewModel>>;
}
