using System.Collections.Generic;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using LanguageExt;

namespace ErsatzTV.Core.Interfaces.Repositories
{
    public interface IFFmpegProfileRepository
    {
        Task<FFmpegProfile> Add(FFmpegProfile ffmpegProfile);
        Task<Option<FFmpegProfile>> Get(int id);
        Task<List<FFmpegProfile>> GetAll();
        Task Update(FFmpegProfile ffmpegProfile);
        Task Delete(int ffmpegProfileId);
    }
}
