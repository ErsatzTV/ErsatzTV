﻿namespace ErsatzTV.Core.FFmpeg;

public enum TempFileCategory
{
    Subtitle = 0,
    SongBackground = 1,
    CoverArt = 2,
    CachedArtwork = 3,

    BadTranscodeFolder = 98,
    BadPlaylist = 99
}
