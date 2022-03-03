using System;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Extensions;

public static class MediaItemExtensions
{
    public static MediaVersion GetHeadVersion(this MediaItem mediaItem) =>
        mediaItem switch
        {
            Movie m => m.MediaVersions.Head(),
            Episode e => e.MediaVersions.Head(),
            MusicVideo mv => mv.MediaVersions.Head(),
            OtherVideo ov => ov.MediaVersions.Head(),
            Song s => s.MediaVersions.Head(),
            _ => throw new ArgumentOutOfRangeException(nameof(mediaItem))
        };
}