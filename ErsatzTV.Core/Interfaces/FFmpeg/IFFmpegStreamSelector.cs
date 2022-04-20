﻿using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Interfaces.FFmpeg;

public interface IFFmpegStreamSelector
{
    Task<MediaStream> SelectVideoStream(Channel channel, MediaVersion version);
    Task<Option<MediaStream>> SelectAudioStream(Channel channel, MediaVersion version);

    Task<Option<Subtitle>> SelectSubtitleStream(Channel channel, MediaVersion version, List<Subtitle> subtitles);
}
