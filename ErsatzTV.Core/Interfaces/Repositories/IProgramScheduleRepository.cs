using System.Collections.Generic;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using LanguageExt;

namespace ErsatzTV.Core.Interfaces.Repositories
{
    public interface IProgramScheduleRepository
    {
        Task<ProgramSchedule> Add(ProgramSchedule programSchedule);
        Task<Option<ProgramSchedule>> Get(int id);
        Task<Option<ProgramSchedule>> GetWithPlayouts(int id);
        Task<List<ProgramSchedule>> GetAll();
        Task Update(ProgramSchedule programSchedule);
        Task Delete(int programScheduleId);
        Task<Option<List<ProgramScheduleItem>>> GetItems(int programScheduleId);
    }
}
