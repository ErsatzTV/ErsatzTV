using System.Diagnostics;

namespace ErsatzTV.Core;

public static class DebugBreak
{
    [DebuggerHidden]
    [Conditional("DEBUG")]
    public static void Break()
    {
        if (Debugger.IsAttached)
        {
            Debugger.Break();
        }
    }
}
