using System.Collections.Generic;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using LanguageExt;

namespace ErsatzTV.Core.Interfaces.Repositories
{
    public interface IResolutionRepository
    {
        Task<Option<Resolution>> Get(int id);
        Task<List<Resolution>> GetAll();
    }
}
