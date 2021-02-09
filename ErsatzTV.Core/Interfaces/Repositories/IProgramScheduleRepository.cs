using System.Collections.Generic;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using LanguageExt;

namespace ErsatzTV.Core.Interfaces.Repositories
{
    public interface IProgramScheduleRepository
    {
        public Task<ProgramSchedule> Add(ProgramSchedule programSchedule);
        public Task<Option<ProgramSchedule>> Get(int id);
        public Task<Option<ProgramSchedule>> GetWithPlayouts(int id);
        public Task<List<ProgramSchedule>> GetAll();
        public Task Update(ProgramSchedule programSchedule);
        public Task Delete(int programScheduleId);
        public Task<Option<List<ProgramScheduleItem>>> GetItems(int programScheduleId);
    }
}
