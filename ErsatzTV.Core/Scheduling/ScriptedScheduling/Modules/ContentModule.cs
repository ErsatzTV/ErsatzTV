using System.Diagnostics.CodeAnalysis;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Scheduling.Engine;
using IronPython.Runtime;

namespace ErsatzTV.Core.Scheduling.ScriptedScheduling.Modules;

[SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores")]
[SuppressMessage("Usage", "VSTHRD002:Avoid problematic synchronous waits")]
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
public class ContentModule(ISchedulingEngine schedulingEngine, CancellationToken cancellationToken)
{
    public void add_search(string key, string query, string order)
    {
        if (!Enum.TryParse(order, ignoreCase: true, out PlaybackOrder playbackOrder))
        {
            return;
        }

        schedulingEngine.AddSearch(key, query, playbackOrder).GetAwaiter().GetResult();
    }

    public void add_collection(string key, string collection, string order)
    {
        if (!Enum.TryParse(order, ignoreCase: true, out PlaybackOrder playbackOrder))
        {
            return;
        }

        schedulingEngine.AddCollection(key, collection, playbackOrder, cancellationToken).GetAwaiter().GetResult();
    }

    public void add_marathon(
        string key,
        string group_by,
        string item_order = "shuffle",
        PythonDictionary guids = null,
        PythonList searches = null,
        bool play_all_items = false,
        bool shuffle_groups = false)
    {

        if (!Enum.TryParse(item_order, ignoreCase: true, out PlaybackOrder itemPlaybackOrder))
        {
            itemPlaybackOrder = PlaybackOrder.Shuffle;
        }

        var mappedGuids = new Dictionary<string, List<string>>();
        if (guids != null)
        {
            foreach (KeyValuePair<object, object> guid in guids)
            {
                var guidKey = guid.Key.ToString();
                if (guidKey is not null && guid.Value is PythonList guidValues)
                {
                    mappedGuids.Add(guidKey, guidValues.Select(x => x.ToString()).ToList());
                }
            }
        }

        var mappedSearches = new List<string>();
        if (searches != null)
        {
            mappedSearches.AddRange(searches.Select(x => x.ToString()));
        }

        // guids OR searches are required
        if (mappedGuids.Count == 0 && mappedSearches.Count == 0)
        {
            return;
        }

        schedulingEngine
            .AddMarathon(key, mappedGuids, mappedSearches, group_by, shuffle_groups, itemPlaybackOrder, play_all_items)
            .GetAwaiter()
            .GetResult();
    }

    public void add_multi_collection(string key, string multi_collection, string order)
    {
        if (!Enum.TryParse(order, ignoreCase: true, out PlaybackOrder playbackOrder))
        {
            return;
        }

        schedulingEngine
            .AddMultiCollection(key, multi_collection, playbackOrder, cancellationToken)
            .GetAwaiter()
            .GetResult();
    }

    public void add_playlist(string key, string playlist, string playlist_group)
    {
        schedulingEngine.AddPlaylist(key, playlist, playlist_group, cancellationToken).GetAwaiter().GetResult();
    }

    public void add_smart_collection(string key, string smart_collection, string order)
    {
        if (!Enum.TryParse(order, ignoreCase: true, out PlaybackOrder playbackOrder))
        {
            return;
        }

        schedulingEngine
            .AddSmartCollection(key, smart_collection, playbackOrder, cancellationToken)
            .GetAwaiter()
            .GetResult();
    }

    public void add_show(string key, PythonDictionary guids, string order)
    {
        if (!Enum.TryParse(order, ignoreCase: true, out PlaybackOrder playbackOrder))
        {
            return;
        }

        schedulingEngine
            .AddShow(key, guids.ToDictionary(k => k.Key.ToString(), k => k.Value.ToString()), playbackOrder)
            .GetAwaiter()
            .GetResult();
    }
}
