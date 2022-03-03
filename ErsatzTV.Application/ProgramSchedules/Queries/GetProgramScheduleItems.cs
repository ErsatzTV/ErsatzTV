using System.Collections.Generic;
using MediatR;

namespace ErsatzTV.Application.ProgramSchedules;

public record GetProgramScheduleItems(int Id) : IRequest<List<ProgramScheduleItemViewModel>>;