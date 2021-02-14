using System;
using System.Linq;
using ErsatzTV.Core.Domain;
using static LanguageExt.Prelude;

namespace ErsatzTV.Application.MediaSources
{
    internal static class Mapper
    {
        internal static MediaSourceViewModel ProjectToViewModel(MediaSource mediaSource) =>
            mediaSource switch
            {
                LocalMediaSource lms => new LocalMediaSourceViewModel(lms.Id, lms.Name, lms.Folder),
                PlexMediaSource pms => ProjectToViewModel(pms),
                _ => throw new NotSupportedException($"Unsupported media source {mediaSource.GetType().Name}")
            };

        internal static PlexMediaSourceViewModel ProjectToViewModel(PlexMediaSource plexMediaSource) =>
            new(
                plexMediaSource.Id,
                plexMediaSource.Name,
                Optional(plexMediaSource.Connections.SingleOrDefault(c => c.IsActive)).Match(c => c.Uri, string.Empty));
    }
}
