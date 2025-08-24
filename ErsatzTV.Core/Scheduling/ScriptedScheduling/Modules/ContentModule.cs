using System.Diagnostics.CodeAnalysis;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Scheduling.Engine;
using IronPython.Runtime;

namespace ErsatzTV.Core.Scheduling.ScriptedScheduling.Modules;

[SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores")]
[SuppressMessage("Usage", "VSTHRD002:Avoid problematic synchronous waits")]
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
public class ContentModule(ISchedulingEngine schedulingEngine)
{
    public bool add_search(string key, string query, string order)
    {
        if (!Enum.TryParse(order, ignoreCase: true, out PlaybackOrder playbackOrder))
        {
            return false;
        }

        schedulingEngine.AddSearch(key, query, playbackOrder).GetAwaiter().GetResult();

        return true;
    }

    public bool add_collection(string key, string collection, string order)
    {
        if (!Enum.TryParse(order, ignoreCase: true, out PlaybackOrder playbackOrder))
        {
            return false;
        }

        schedulingEngine.AddCollection(key, collection, playbackOrder).GetAwaiter().GetResult();

        return true;
    }

    public bool add_multi_collection(string key, string multi_collection, string order)
    {
        if (!Enum.TryParse(order, ignoreCase: true, out PlaybackOrder playbackOrder))
        {
            return false;
        }

        schedulingEngine.AddMultiCollection(key, multi_collection, playbackOrder).GetAwaiter().GetResult();

        return true;
    }

    public bool add_smart_collection(string key, string smart_collection, string order)
    {
        if (!Enum.TryParse(order, ignoreCase: true, out PlaybackOrder playbackOrder))
        {
            return false;
        }

        schedulingEngine.AddSmartCollection(key, smart_collection, playbackOrder).GetAwaiter().GetResult();

        return true;
    }

    public bool add_show(string key, PythonDictionary guids, string order)
    {
        if (!Enum.TryParse(order, ignoreCase: true, out PlaybackOrder playbackOrder))
        {
            return false;
        }

        schedulingEngine
            .AddShow(key, guids.ToDictionary(k => k.Key.ToString(), k => k.Value.ToString()), playbackOrder)
            .GetAwaiter()
            .GetResult();

        return true;
    }
}
