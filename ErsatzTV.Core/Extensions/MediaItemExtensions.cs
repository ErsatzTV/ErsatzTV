﻿using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Emby;
using ErsatzTV.Core.Interfaces.Jellyfin;
using ErsatzTV.Core.Interfaces.Plex;

namespace ErsatzTV.Core.Extensions;

public static class MediaItemExtensions
{
    public static Option<TimeSpan> GetNonZeroDuration(this MediaItem mediaItem)
    {
        Option<TimeSpan> maybeDuration = mediaItem switch
        {
            Movie m => m.MediaVersions.HeadOrNone().Map(v => v.Duration),
            Episode e => e.MediaVersions.HeadOrNone().Map(v => v.Duration),
            MusicVideo mv => mv.MediaVersions.HeadOrNone().Map(v => v.Duration),
            OtherVideo ov => ov.MediaVersions.HeadOrNone().Map(v => v.Duration),
            Song s => s.MediaVersions.HeadOrNone().Map(v => v.Duration),
            ChapterMediaItem c => c.MediaVersion.Duration,
            _ => None
        };

        // zero duration is invalid, so return none in that case
        return maybeDuration.Any(duration => duration == TimeSpan.Zero) ? Option<TimeSpan>.None : maybeDuration;
    }

    public static TimeSpan GetDurationForPlayout(this MediaItem mediaItem)
    {
        if (mediaItem is Image image)
        {
            return TimeSpan.FromSeconds(image.ImageMetadata.Head().DurationSeconds ?? Image.DefaultSeconds);
        }

        MediaVersion version = mediaItem.GetHeadVersion();

        if (mediaItem is RemoteStream remoteStream)
        {
            return version.Duration == TimeSpan.Zero && remoteStream.Duration.HasValue
                ? remoteStream.Duration.Value
                : version.Duration;
        }

        return version.Duration;
    }

    public static MediaVersion GetHeadVersion(this MediaItem mediaItem) =>
        mediaItem switch
        {
            Movie m => m.MediaVersions.Head(),
            Episode e => e.MediaVersions.Head(),
            MusicVideo mv => mv.MediaVersions.Head(),
            OtherVideo ov => ov.MediaVersions.Head(),
            Song s => s.MediaVersions.Head(),
            Image i => i.MediaVersions.Head(),
            RemoteStream rs => rs.MediaVersions.Head(),
            ChapterMediaItem c => c.MediaVersion,
            _ => throw new ArgumentOutOfRangeException(nameof(mediaItem))
        };

    public static async Task<string> GetLocalPath(
        this MediaItem mediaItem,
        IPlexPathReplacementService plexPathReplacementService,
        IJellyfinPathReplacementService jellyfinPathReplacementService,
        IEmbyPathReplacementService embyPathReplacementService,
        CancellationToken cancellationToken,
        bool log = true)
    {
        MediaVersion version = mediaItem.GetHeadVersion();

        MediaFile file = version.MediaFiles.Head();
        string path = file.Path;
        return mediaItem switch
        {
            PlexMovie plexMovie => await plexPathReplacementService.GetReplacementPlexPath(
                plexMovie.LibraryPathId,
                path,
                cancellationToken,
                log),
            PlexEpisode plexEpisode => await plexPathReplacementService.GetReplacementPlexPath(
                plexEpisode.LibraryPathId,
                path,
                cancellationToken,
                log),
            JellyfinMovie jellyfinMovie => await jellyfinPathReplacementService.GetReplacementJellyfinPath(
                jellyfinMovie.LibraryPathId,
                path,
                cancellationToken,
                log),
            JellyfinEpisode jellyfinEpisode => await jellyfinPathReplacementService.GetReplacementJellyfinPath(
                jellyfinEpisode.LibraryPathId,
                path,
                cancellationToken,
                log),
            EmbyMovie embyMovie => await embyPathReplacementService.GetReplacementEmbyPath(
                embyMovie.LibraryPathId,
                path,
                cancellationToken,
                log),
            EmbyEpisode embyEpisode => await embyPathReplacementService.GetReplacementEmbyPath(
                embyEpisode.LibraryPathId,
                path,
                cancellationToken,
                log),
            _ => path
        };
    }
}
