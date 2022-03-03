using System.Collections.Generic;
using MediatR;

namespace ErsatzTV.Application.ProgramSchedules;

public record GetAllProgramSchedules : IRequest<List<ProgramScheduleViewModel>>;