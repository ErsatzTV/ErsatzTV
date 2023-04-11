using System.Diagnostics;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Scheduling;

[DebuggerDisplay("{" + nameof(First) + "}")]
public class GroupedMediaItem : IEquatable<GroupedMediaItem>
{
    public GroupedMediaItem()
    {
    }

    public GroupedMediaItem(MediaItem first, List<MediaItem> additional)
    {
        First = first;
        Additional = additional ?? new List<MediaItem>();
    }

    public MediaItem First { get; init; }
    public List<MediaItem> Additional { get; protected init; }

    public bool Equals(GroupedMediaItem other) =>
        Equals(First.Id, other?.First.Id) && Equals(Additional?.Count, other?.Additional?.Count) &&
        Equals(Additional?.Map(x => x.Id), other?.Additional?.Map(x => x.Id));

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj))
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj.GetType() != GetType())
        {
            return false;
        }

        return Equals((GroupedMediaItem)obj);
    }

    public override int GetHashCode() => HashCode.Combine(First.Id, Additional?.Map(x => x.Id));

    public static IList<MediaItem> FlattenGroups(IEnumerable<GroupedMediaItem> copy, int mediaItemCount)
    {
        var result = new MediaItem[mediaItemCount];
        var i = 0;
        foreach (GroupedMediaItem group in copy)
        {
            result[i++] = group.First;
            foreach (MediaItem additional in Optional(group.Additional).Flatten())
            {
                result[i++] = additional;
            }
        }

        return result;
    }
}
