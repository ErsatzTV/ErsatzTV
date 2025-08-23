using System.Diagnostics.CodeAnalysis;
using ErsatzTV.Core.Interfaces.Scheduling;

namespace ErsatzTV.Core.Scheduling.ScriptedScheduling.Modules;

[SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores")]
public class ContentModule
{
    private Dictionary<string, IMediaCollectionEnumerator> _contentEnumerators = [];

    public bool add_collection(string key, string name, string order)
    {
        if (_contentEnumerators.ContainsKey(key))
        {
            return false;
        }

        Console.WriteLine($"Adding collection '{name}' with key '{key}' and order '{order}'");
        _contentEnumerators.Clear();

        return true;
    }
}
