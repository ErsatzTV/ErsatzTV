using ErsatzTV.Core.Domain;
using Newtonsoft.Json;

namespace ErsatzTV.Core.Extensions;

public static class ProgramScheduleItemExtensions
{
    public static ProgramScheduleItem DeepCopy(this ProgramScheduleItem item)
    {
        if (item == null)
        {
            return null;
        }

        var settings = new JsonSerializerSettings
        {
            // program schedule item => graphics element => (same) program schedule item should be ignored
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore
        };

        string json = JsonConvert.SerializeObject(item, settings);
        return (ProgramScheduleItem)JsonConvert.DeserializeObject(json, item.GetType(), settings);
    }
}
