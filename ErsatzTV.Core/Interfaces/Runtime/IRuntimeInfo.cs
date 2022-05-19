using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace ErsatzTV.Core.Interfaces.Runtime;

public interface IRuntimeInfo
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    bool IsOSPlatform(OSPlatform osPlatform);
}
