using System.Runtime.InteropServices;
using ErsatzTV.FFmpeg.Runtime;

namespace ErsatzTV.Infrastructure.Runtime;

public class RuntimeInfo : IRuntimeInfo
{
    public bool IsOSPlatform(OSPlatform osPlatform) => RuntimeInformation.IsOSPlatform(osPlatform);
}
