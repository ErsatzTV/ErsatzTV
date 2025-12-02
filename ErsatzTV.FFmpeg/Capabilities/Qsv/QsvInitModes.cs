using System.Runtime.InteropServices;
using ErsatzTV.FFmpeg.Runtime;

namespace ErsatzTV.FFmpeg.Capabilities.Qsv;

public static class QsvInitModes
{
    public static IEnumerable<QsvInitMode> GetModesToTest(IRuntimeInfo runtimeInfo)
    {
        yield return QsvInitMode.None;

        if (runtimeInfo.IsOSPlatform(OSPlatform.Windows))
        {
            yield return QsvInitMode.D3d11Va;
        }

        yield return QsvInitMode.Qsv;
    }
}
