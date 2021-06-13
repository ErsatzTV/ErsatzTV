using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using LanguageExt;

namespace ErsatzTV.Core.Interfaces.Repositories
{
    public interface IPlayoutRepository
    {
        Task<Option<Playout>> GetFull(int id);
        Task Update(Playout playout);
    }
}
