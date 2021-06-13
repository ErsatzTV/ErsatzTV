using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using LanguageExt;

namespace ErsatzTV.Core.Interfaces.Repositories
{
    public interface IFFmpegProfileRepository
    {
        Task<Option<FFmpegProfile>> Get(int id);
        Task<FFmpegProfile> Copy(int ffmpegProfileId, string name);
    }
}
