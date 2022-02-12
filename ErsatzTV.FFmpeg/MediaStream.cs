﻿using ErsatzTV.FFmpeg.PixelFormat;

namespace ErsatzTV.FFmpeg;

public record MediaStream(int Index, string Codec, StreamKind Kind);

public record AudioStream(int Index, string Codec) : MediaStream(Index, Codec, StreamKind.Audio);

public record VideoStream(int Index, string Codec, IPixelFormat PixelFormat) : MediaStream(
    Index,
    Codec,
    StreamKind.Video);
