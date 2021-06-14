using System.Collections.Generic;
using MediatR;

namespace ErsatzTV.Application.ProgramSchedules.Queries
{
    public record GetProgramScheduleItems(int Id) : IRequest<List<ProgramScheduleItemViewModel>>;
}
