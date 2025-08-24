using System.Diagnostics.CodeAnalysis;
using ErsatzTV.Core.Domain.Filler;
using ErsatzTV.Core.Scheduling.Engine;
using IronPython.Runtime;

namespace ErsatzTV.Core.Scheduling.ScriptedScheduling.Modules;

[SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores")]
[SuppressMessage("Usage", "VSTHRD002:Avoid problematic synchronous waits")]
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
public class PlayoutModule(ISchedulingEngine schedulingEngine)
{
    public int FailureCount { get; private set; }

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

        bool success = schedulingEngine.AddCount(content, count, maybeFillerKind, custom_title, disable_watermarks);
        if (success)
        {
            FailureCount = 0;
        }
        else
        {
            FailureCount++;
        }
    }


    // control instructions

    public void start_epg_group(bool advance = true)
    {
        schedulingEngine.LockGuideGroup(advance);
    }

    public void stop_epg_group()
    {
        schedulingEngine.UnlockGuideGroup();
    }

    public void graphics_on(string graphics, PythonDictionary variables = null)
    {
        var maybeVariables = new Dictionary<string, string>();
        if (variables != null)
        {
            maybeVariables = variables.ToDictionary(v => v.Key.ToString(), v => v.Value.ToString());
        }

        schedulingEngine
            .GraphicsOn([graphics], maybeVariables)
            .GetAwaiter()
            .GetResult();
    }

    public void graphics_on(PythonList graphics, PythonDictionary variables = null)
    {
        var maybeVariables = new Dictionary<string, string>();
        if (variables != null)
        {
            maybeVariables = variables.ToDictionary(v => v.Key.ToString(), v => v.Value.ToString());
        }

        schedulingEngine
            .GraphicsOn(
                graphics.Select(g => g.ToString()).ToList(),
                maybeVariables)
            .GetAwaiter()
            .GetResult();
    }

    public void graphics_off(string graphics = null)
    {
        if (string.IsNullOrWhiteSpace(graphics))
        {
            schedulingEngine.GraphicsOff([]).GetAwaiter().GetResult();
        }
        else
        {
            schedulingEngine.GraphicsOff([graphics]).GetAwaiter().GetResult();
        }
    }

    public void graphics_off(PythonList graphics)
    {
        schedulingEngine.GraphicsOff(graphics.Select(g => g.ToString()).ToList()).GetAwaiter().GetResult();
    }

    public void skip_items(string content, int count)
    {
        schedulingEngine.SkipItems(content, count);
    }

    public void wait_until(string when, bool tomorrow = false, bool rewind_on_reset = false)
    {
        if (TimeOnly.TryParse(when, out TimeOnly waitUntil))
        {
            schedulingEngine.WaitUntil(waitUntil, tomorrow, rewind_on_reset);
        }
    }
}
