﻿namespace ErsatzTV.Core.Domain;

public enum PlaybackOrder
{
    None = 0,

    Chronological = 1,
    Random = 2,
    Shuffle = 3,
    ShuffleInOrder = 4,
    MultiEpisodeShuffle = 5,
    SeasonEpisode = 6,
    RandomRotation = 7
}
