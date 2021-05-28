using System.Collections.Generic;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using LanguageExt;

namespace ErsatzTV.Core.Interfaces.Repositories
{
    public interface IArtworkRepository
    {
        Task<List<Artwork>> GetOrphanedArtwork();
        Task<Unit> Delete(List<Artwork> artwork);
    }
}
