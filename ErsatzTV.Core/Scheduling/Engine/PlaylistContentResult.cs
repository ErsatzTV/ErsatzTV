using System.Collections.Immutable;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Scheduling.Engine;

public record PlaylistContentResult(
    PlaylistEnumerator PlaylistEnumerator,
    ImmutableDictionary<CollectionKey, List<MediaItem>> Content);
