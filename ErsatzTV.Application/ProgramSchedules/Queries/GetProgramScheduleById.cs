using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.ProgramSchedules.Queries
{
    public record GetProgramScheduleById(int Id) : IRequest<Option<ProgramScheduleViewModel>>;
}
