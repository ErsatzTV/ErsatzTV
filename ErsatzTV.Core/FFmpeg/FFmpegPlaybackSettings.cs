using System;
using System.Collections.Generic;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.FFmpeg;
using LanguageExt;

namespace ErsatzTV.Core.FFmpeg
{
    public class FFmpegPlaybackSettings
    {
        public int ThreadCount { get; set; }
        public List<string> FormatFlags { get; set; }
        public HardwareAccelerationKind HardwareAcceleration { get; set; }
        public string VideoDecoder { get; set; }
        public bool RealtimeOutput => true;
        public Option<TimeSpan> StreamSeek { get; set; }
        public Option<IDisplaySize> ScaledSize { get; set; }
        public bool PadToDesiredResolution { get; set; }
        public string VideoCodec { get; set; }
        public Option<int> VideoBitrate { get; set; }
        public Option<int> VideoBufferSize { get; set; }
        public Option<int> AudioBitrate { get; set; }
        public Option<int> AudioBufferSize { get; set; }
        public Option<int> AudioChannels { get; set; }
        public Option<int> AudioSampleRate { get; set; }
        public Option<TimeSpan> AudioDuration { get; set; }
        public string AudioCodec { get; set; }
        public bool Deinterlace { get; set; }
        public Option<string> FrameRate { get; set; }
    }
}
