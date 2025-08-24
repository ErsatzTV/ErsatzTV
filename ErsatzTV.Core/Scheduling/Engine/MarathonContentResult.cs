using System.Collections.Immutable;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Scheduling.Engine;

public record MarathonContentResult(
    PlaylistEnumerator PlaylistEnumerator,
    ImmutableDictionary<CollectionKey, List<MediaItem>> Content);
