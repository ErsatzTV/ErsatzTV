using System.Collections.Generic;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using LanguageExt;

namespace ErsatzTV.Core.Interfaces.Repositories
{
    public interface IResolutionRepository
    {
        public Task<Option<Resolution>> Get(int id);
        public Task<List<Resolution>> GetAll();
    }
}
