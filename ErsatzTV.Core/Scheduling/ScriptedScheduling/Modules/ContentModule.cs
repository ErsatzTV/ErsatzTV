using System.Diagnostics.CodeAnalysis;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Scheduling.Engine;

namespace ErsatzTV.Core.Scheduling.ScriptedScheduling.Modules;

[SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores")]
[SuppressMessage("Usage", "VSTHRD002:Avoid problematic synchronous waits")]
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
}
