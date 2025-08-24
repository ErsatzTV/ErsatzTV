using System.Diagnostics.CodeAnalysis;
using ErsatzTV.Core.Domain.Filler;
using ErsatzTV.Core.Scheduling.Engine;

namespace ErsatzTV.Core.Scheduling.ScriptedScheduling.Modules;

[SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores")]
[SuppressMessage("Usage", "VSTHRD002:Avoid problematic synchronous waits")]
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
public class PlayoutModule(ISchedulingEngine schedulingEngine)
{
    // content instructions

    public void add_count(
        string content,
        int count,
        string filler_kind = null,
        string custom_title = null,
        bool disable_watermarks = false)
    {
        Option<FillerKind> maybeFillerKind = Option<FillerKind>.None;
        if (Enum.TryParse(filler_kind, ignoreCase: true, out FillerKind fillerKind))
        {
            maybeFillerKind = fillerKind;
        }

        schedulingEngine.AddCount(content, count, maybeFillerKind, custom_title, disable_watermarks);
    }


    // control instructions

    public void wait_until(string when, bool tomorrow = false, bool rewind_on_reset = false)
    {
        if (TimeOnly.TryParse(when, out TimeOnly waitUntil))
        {
            schedulingEngine.WaitUntil(waitUntil, tomorrow, rewind_on_reset);
        }
    }
}
