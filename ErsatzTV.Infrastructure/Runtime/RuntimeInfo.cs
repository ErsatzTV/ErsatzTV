using System.Runtime.InteropServices;
using ErsatzTV.Core.Interfaces.Runtime;

namespace ErsatzTV.Infrastructure.Runtime;

public class RuntimeInfo : IRuntimeInfo
{
    public bool IsOSPlatform(OSPlatform osPlatform) => RuntimeInformation.IsOSPlatform(osPlatform);
}
