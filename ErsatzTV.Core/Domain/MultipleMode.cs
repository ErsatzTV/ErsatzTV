namespace ErsatzTV.Core.Domain;

public enum MultipleMode
{
    // static integer count
    Count = 0,

    // current size of the collection
    CollectionSize = 1,

    // current size of the playlist item
    PlaylistItemSize = 2,

    // from one item (not a multi-episode) to however many multi-episodes are linked together
    // is this limited to chronological and season/episode?
    MultiEpisodeGroupSize = 3
}
