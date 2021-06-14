using System.Collections.Generic;
using System.Threading.Tasks;

namespace ErsatzTV.Core.Interfaces.Repositories
{
    public interface IMediaItemRepository
    {
        Task<List<string>> GetAllLanguageCodes();
    }
}
