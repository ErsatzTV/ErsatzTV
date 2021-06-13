using System.Collections.Generic;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using LanguageExt;

namespace ErsatzTV.Core.Interfaces.Repositories
{
    public interface IProgramScheduleRepository
    {
        Task<Option<ProgramSchedule>> GetWithPlayouts(int id);
        Task Update(ProgramSchedule programSchedule);
        Task<Option<List<ProgramScheduleItem>>> GetItems(int programScheduleId);
    }
}
