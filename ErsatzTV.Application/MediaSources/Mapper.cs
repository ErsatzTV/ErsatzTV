using System;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.MediaSources
{
    internal static class Mapper
    {
        internal static MediaSourceViewModel ProjectToViewModel(MediaSource mediaSource) =>
            mediaSource switch
            {
                // TODO: address this...
                LocalMediaSource lms => new LocalMediaSourceViewModel(lms.Id, "LMS Name", "LMS Folder"),
                PlexMediaSource pms => Plex.Mapper.ProjectToViewModel(pms),
                _ => throw new NotSupportedException($"Unsupported media source {mediaSource.GetType().Name}")
            };
    }
}
