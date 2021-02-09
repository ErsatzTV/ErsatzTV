using System.Collections.Generic;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using LanguageExt;

namespace ErsatzTV.Core.Interfaces.Repositories
{
    public interface IFFmpegProfileRepository
    {
        public Task<FFmpegProfile> Add(FFmpegProfile ffmpegProfile);
        public Task<Option<FFmpegProfile>> Get(int id);
        public Task<List<FFmpegProfile>> GetAll();
        public Task Update(FFmpegProfile ffmpegProfile);
        public Task Delete(int ffmpegProfileId);
    }
}
