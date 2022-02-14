using System.Threading.Tasks;
using ErsatzTV.Core.Interfaces.FFmpeg;

namespace ErsatzTV.Core.FFmpeg;

public interface IFFmpegProcessServiceFactory
{
    Task<IFFmpegProcessService> GetService();
}
