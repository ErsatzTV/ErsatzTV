using System.Collections.Immutable;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Scheduling;

namespace ErsatzTV.Core.Scheduling.YamlScheduling;

public record YamlMarathonContentResult(
    IMediaCollectionEnumerator PlaylistEnumerator,
    ImmutableDictionary<CollectionKey, List<MediaItem>> Content);
