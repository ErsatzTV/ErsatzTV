using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.Troubleshooting;

public record PlayoutHistoryDetailsViewModel(
    PlaybackOrder PlaybackOrder,
    CollectionType CollectionType,
    string Name,
    string MediaItemType,
    string MediaItemTitle);
