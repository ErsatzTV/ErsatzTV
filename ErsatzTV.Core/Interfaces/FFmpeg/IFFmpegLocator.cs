using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using LanguageExt;

namespace ErsatzTV.Core.Interfaces.FFmpeg
{
    public interface IFFmpegLocator
    {
        Task<Option<string>> ValidatePath(string executableBase, ConfigElementKey key);
    }
}
