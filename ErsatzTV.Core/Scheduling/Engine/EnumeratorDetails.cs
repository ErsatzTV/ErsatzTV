using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Scheduling;

namespace ErsatzTV.Core.Scheduling.Engine;

public record EnumeratorDetails(
    IMediaCollectionEnumerator Enumerator,
    string HistoryKey,
    PlaybackOrder PlaybackOrder);
