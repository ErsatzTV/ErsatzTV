using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using LanguageExt;

namespace ErsatzTV.Core.Interfaces.Repositories
{
    public interface ICollectionRepository
    {
        public Task<Option<Collection>> GetWithItemsUntracked(int collectionId);
    }
}
