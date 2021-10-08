// zlib License
//
// Copyright (c) 2021 Dan Ferguson, Victor Hugo Soliz Kuncar, Jason Dove
//
// This software is provided 'as-is', without any express or implied
// warranty. In no event will the authors be held liable for any damages
// arising from the use of this software.
//
// Permission is granted to anyone to use this software for any purpose,
// including commercial applications, and to alter it and redistribute it
// freely, subject to the following restrictions:
//
// 1. The origin of this software must not be misrepresented; you must not
//    claim that you wrote the original software. If you use this software
//    in a product, an acknowledgment in the product documentation would be
//    appreciated but is not required.
// 2. Altered source versions must be plainly marked as such, and must not be
//    misrepresented as being the original software.
// 3. This notice may not be removed or altered from any source distribution.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.FFmpeg;
using LanguageExt;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Core.FFmpeg
{
    internal class FFmpegProcessBuilder
    {
        private static readonly Dictionary<string, string> QsvMap = new()
        {
            { "h264", "h264_qsv" },
            { "hevc", "hevc_qsv" },
            { "mpeg2video", "mpeg2_qsv" }
        };

        private readonly List<string> _arguments = new();
        private readonly string _ffmpegPath;
        private readonly bool _saveReports;
        private readonly ILogger _logger;
        private FFmpegComplexFilterBuilder _complexFilterBuilder = new();
        private bool _isConcat;
        private VaapiDriver _vaapiDriver;
        private HardwareAccelerationKind _hwAccel;

        public FFmpegProcessBuilder(string ffmpegPath, bool saveReports, ILogger logger)
        {
            _ffmpegPath = ffmpegPath;
            _saveReports = saveReports;
            _logger = logger;
        }

        public FFmpegProcessBuilder WithVaapiDriver(Option<VaapiDriver> maybeVaapiDriver)
        {
            foreach (VaapiDriver vaapiDriver in maybeVaapiDriver)
            {
                _vaapiDriver = vaapiDriver;
            }

            return this;
        }

        public FFmpegProcessBuilder WithThreads(int threads)
        {
            _arguments.Add("-threads");
            _arguments.Add($"{threads}");
            return this;
        }

        public FFmpegProcessBuilder WithHardwareAcceleration(HardwareAccelerationKind hwAccel)
        {
            _hwAccel = hwAccel;

            switch (hwAccel)
            {
                case HardwareAccelerationKind.Qsv:
                    _arguments.Add("-hwaccel");
                    _arguments.Add("qsv");
                    _arguments.Add("-init_hw_device");
                    _arguments.Add("qsv=qsv:MFX_IMPL_hw_any");
                    break;
                case HardwareAccelerationKind.Nvenc:
                    _arguments.Add("-hwaccel");
                    _arguments.Add("cuda");
                    _arguments.Add("-hwaccel_output_format");
                    _arguments.Add("cuda");
                    break;
                case HardwareAccelerationKind.Vaapi:
                    _arguments.Add("-hwaccel");
                    _arguments.Add("vaapi");
                    _arguments.Add("-vaapi_device");
                    _arguments.Add("/dev/dri/renderD128");
                    _arguments.Add("-hwaccel_output_format");
                    _arguments.Add("vaapi");
                    break;
            }

            _complexFilterBuilder = _complexFilterBuilder.WithHardwareAcceleration(hwAccel);

            return this;
        }

        public FFmpegProcessBuilder WithRealtimeOutput(bool realtimeOutput)
        {
            if (realtimeOutput)
            {
                _arguments.Add("-re");
            }

            return this;
        }

        public FFmpegProcessBuilder WithSeek(Option<TimeSpan> maybeStart)
        {
            maybeStart.IfSome(
                start =>
                {
                    _arguments.Add("-ss");
                    _arguments.Add($"{start:c}");
                });

            return this;
        }

        public FFmpegProcessBuilder WithInfiniteLoop()
        {
            _arguments.Add("-stream_loop");
            _arguments.Add("-1");
            return this;
        }

        public FFmpegProcessBuilder WithLoopedImage(string input)
        {
            _arguments.Add("-loop");
            _arguments.Add("1");
            return WithInput(input);
        }


        public FFmpegProcessBuilder WithPipe()
        {
            _arguments.Add("pipe:1");
            return this;
        }

        public FFmpegProcessBuilder WithPixfmt(string pixfmt)
        {
            _arguments.Add("-pix_fmt");
            _arguments.Add(pixfmt);
            return this;
        }

        public FFmpegProcessBuilder WithLibavfilter()
        {
            _arguments.Add("-f");
            _arguments.Add("lavfi");
            return this;
        }

        public FFmpegProcessBuilder WithInput(string input)
        {
            _arguments.Add("-i");
            _arguments.Add($"{input}");
            return this;
        }

        public FFmpegProcessBuilder WithWatermark(
            Option<ChannelWatermark> watermark,
            Option<string> maybePath,
            IDisplaySize resolution,
            bool isAnimated)
        {
            foreach (string path in maybePath)
            {
                if (isAnimated)
                {
                    _arguments.Add("-ignore_loop");
                    _arguments.Add("0");
                }

                _arguments.Add("-i");
                _arguments.Add(path);

                _complexFilterBuilder = _complexFilterBuilder.WithWatermark(watermark, resolution);
            }

            return this;
        }

        public FFmpegProcessBuilder WithInputCodec(string input, HardwareAccelerationKind hwAccel, string codec, string pixelFormat)
        {
            if (hwAccel == HardwareAccelerationKind.Qsv && QsvMap.TryGetValue(codec, out string qsvCodec))
            {
                _arguments.Add("-c:v");
                _arguments.Add(qsvCodec);
            }

            _complexFilterBuilder = _complexFilterBuilder
                .WithInputCodec(codec)
                .WithInputPixelFormat(pixelFormat);

            _arguments.Add("-i");
            _arguments.Add($"{input}");
            return this;
        }

        public FFmpegProcessBuilder WithFiltergraph(string graph)
        {
            _arguments.Add("-vf");
            _arguments.Add($"{graph}");
            return this;
        }

        public FFmpegProcessBuilder WithFilterComplex(string filter, string finalVideo, string finalAudio)
        {
            _arguments.Add("-filter_complex");
            _arguments.Add($"{filter}");
            _arguments.Add("-map");
            _arguments.Add(finalVideo);
            _arguments.Add("-map");
            _arguments.Add(finalAudio);
            return this;
        }

        public FFmpegProcessBuilder WithConcat(string concatPlaylist)
        {
            _isConcat = true;

            var arguments = new List<string>
            {
                "-f", "concat",
                "-safe", "0",
                "-protocol_whitelist", "file,http,tcp,https,tcp,tls",
                "-probesize", "32",
                "-i", concatPlaylist,
                "-c", "copy",
                "-muxdelay", "0",
                "-muxpreload", "0"
                // "-avoid_negative_ts", "make_zero"
            };
            _arguments.AddRange(arguments);
            return this;
        }

        public FFmpegProcessBuilder WithMetadata(Channel channel, Option<MediaStream> maybeAudioStream)
        {
            if (channel.StreamingMode == StreamingMode.TransportStream)
            {
                _arguments.AddRange(new[] { "-map_metadata", "-1" });
            }

            foreach (MediaStream audioStream in maybeAudioStream)
            {
                if (!string.IsNullOrWhiteSpace(audioStream.Language))
                {
                    _arguments.AddRange(new[] { "-metadata:s:a:0", $"language={audioStream.Language}" });
                }
            }

            var arguments = new List<string>
            {
                "-metadata", "service_provider=\"ErsatzTV\"",
                "-metadata", $"service_name=\"{channel.Name}\""
            };

            _arguments.AddRange(arguments);

            return this;
        }

        public FFmpegProcessBuilder WithFormatFlags(IEnumerable<string> formatFlags)
        {
            _arguments.Add("-fflags");
            _arguments.Add(string.Join(string.Empty, formatFlags));
            return this;
        }

        public FFmpegProcessBuilder WithErrorText(IDisplaySize desiredResolution, string text)
        {
            string fontPath = Path.Combine(FileSystemLayout.ResourcesCacheFolder, "Roboto-Regular.ttf");
            var fontFile = $"fontfile={fontPath}";
            const string FONT_COLOR = "fontcolor=white";
            const string X = "x=(w-text_w)/2";
            const string Y = "y=(h-text_h)/3*2";

            string fontSize = text.Length > 80 ? "fontsize=30" : text.Length > 60 ? "fontsize=40" : "fontsize=60";

            return WithFilterComplex(
                $"[0:0]scale={desiredResolution.Width}:{desiredResolution.Height},drawtext={fontFile}:{fontSize}:{FONT_COLOR}:{X}:{Y}:text='{text}'[v]",
                "[v]",
                "1:a");
        }

        public FFmpegProcessBuilder WithDuration(TimeSpan duration)
        {
            _arguments.Add("-t");
            _arguments.Add($"{duration:c}");
            return this;
        }

        public FFmpegProcessBuilder WithFormat(string format)
        {
            _arguments.Add("-f");
            _arguments.Add($"{format}");
            return this;
        }

        public FFmpegProcessBuilder WithHls(string channelNumber, MediaVersion mediaVersion)
        {
            if (!int.TryParse(mediaVersion.RFrameRate, out int frameRate))
            {
                string[] split = mediaVersion.RFrameRate.Split("/");
                if (int.TryParse(split[0], out int left) && int.TryParse(split[1], out int right))
                {
                    frameRate = (int)Math.Round(left / (double)right);
                }
                else
                {
                    _logger.LogInformation("Unable to detect framerate, using {FrameRate}", 24);
                    frameRate = 24;
                }
            }

            _arguments.AddRange(
                new[]
                {
                    "-g", $"{frameRate * 2}",
                    "-keyint_min", $"{frameRate * 2}",
                    // "-force_key_frames",
                    // "expr:gte(t,n_forced*2)",
                    "-f", "hls",
                    "-hls_time", "2",
                    "-hls_list_size", "10",
                    "-segment_list_flags", "+live",
                    "-hls_flags", "delete_segments+program_date_time+append_list+discont_start+omit_endlist",
                    Path.Combine(FileSystemLayout.TranscodeFolder, channelNumber, "live.m3u8")
                });

            return this;
        }

        public FFmpegProcessBuilder WithPlaybackArgs(FFmpegPlaybackSettings playbackSettings)
        {
            var arguments = new List<string>
            {
                "-c:v", playbackSettings.VideoCodec,
                "-flags", "cgop",
                "-sc_threshold", "0" // disable scene change detection
            };

            string[] videoBitrateArgs = playbackSettings.VideoBitrate.Match(
                bitrate =>
                    new[]
                    {
                        "-b:v", $"{bitrate}k",
                        "-maxrate:v", $"{bitrate}k"
                    },
                Array.Empty<string>());
            arguments.AddRange(videoBitrateArgs);

            playbackSettings.VideoBufferSize
                .IfSome(bufferSize => arguments.AddRange(new[] { "-bufsize:v", $"{bufferSize}k" }));

            string[] audioBitrateArgs = playbackSettings.AudioBitrate.Match(
                bitrate =>
                    new[]
                    {
                        "-b:a", $"{bitrate}k",
                        "-maxrate:a", $"{bitrate}k"
                    },
                Array.Empty<string>());
            arguments.AddRange(audioBitrateArgs);

            playbackSettings.AudioBufferSize
                .IfSome(bufferSize => arguments.AddRange(new[] { "-bufsize:a", $"{bufferSize}k" }));

            playbackSettings.AudioChannels
                .IfSome(channels => arguments.AddRange(new[] { "-ac", $"{channels}" }));

            playbackSettings.AudioSampleRate
                .IfSome(sampleRate => arguments.AddRange(new[] { "-ar", $"{sampleRate}k" }));

            arguments.AddRange(
                new[]
                {
                    "-c:a", playbackSettings.AudioCodec,
                    "-movflags", "+faststart",
                    "-muxdelay", "0",
                    "-muxpreload", "0"
                });

            _arguments.AddRange(arguments);
            return this;
        }

        public FFmpegProcessBuilder WithScaling(IDisplaySize displaySize)
        {
            _complexFilterBuilder = _complexFilterBuilder.WithScaling(displaySize);
            return this;
        }

        public FFmpegProcessBuilder WithBlackBars(IDisplaySize displaySize)
        {
            _complexFilterBuilder = _complexFilterBuilder.WithBlackBars(displaySize);
            return this;
        }

        public FFmpegProcessBuilder WithAlignedAudio(Option<TimeSpan> audioDuration)
        {
            _complexFilterBuilder = _complexFilterBuilder.WithAlignedAudio(audioDuration);
            return this;
        }

        public FFmpegProcessBuilder WithNormalizeLoudness(bool normalizeLoudness)
        {
            _complexFilterBuilder = _complexFilterBuilder.WithNormalizeLoudness(normalizeLoudness);
            return this;
        }

        public FFmpegProcessBuilder WithVideoTrackTimeScale(Option<int> videoTrackTimeScale)
        {
            videoTrackTimeScale.IfSome(
                timeScale =>
                {
                    _arguments.Add("-video_track_timescale");
                    _arguments.Add($"{timeScale}");
                });
            return this;
        }

        public FFmpegProcessBuilder WithDeinterlace(bool deinterlace)
        {
            _complexFilterBuilder = _complexFilterBuilder.WithDeinterlace(deinterlace);
            return this;
        }

        public FFmpegProcessBuilder WithFilterComplex(
            MediaStream videoStream,
            Option<MediaStream> maybeAudioStream,
            string videoCodec)
        {
            _complexFilterBuilder = _complexFilterBuilder.WithVideoEncoder(videoCodec);

            int videoStreamIndex = videoStream.Index;
            Option<int> maybeIndex = maybeAudioStream.Map(ms => ms.Index);

            var videoLabel = $"0:{videoStreamIndex}";
            var audioLabel = $"0:{maybeIndex.Match(i => i.ToString(), () => "a")}";

            Option<FFmpegComplexFilter> maybeFilter = _complexFilterBuilder.Build(videoStreamIndex, maybeIndex);
            maybeFilter.IfSome(
                filter =>
                {
                    _arguments.Add("-filter_complex");
                    _arguments.Add(filter.ComplexFilter);
                    videoLabel = filter.VideoLabel;
                    audioLabel = filter.AudioLabel;
                });

            _arguments.Add("-map");
            _arguments.Add(videoLabel);

            _arguments.Add("-map");
            _arguments.Add(audioLabel);

            return this;
        }

        public FFmpegProcessBuilder WithQuiet()
        {
            _arguments.AddRange(new[] { "-hide_banner", "-loglevel", "error", "-nostats" });
            return this;
        }

        public Process Build()
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = _ffmpegPath,
                RedirectStandardOutput = true,
                RedirectStandardError = false,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8
            };

            if (_hwAccel == HardwareAccelerationKind.Vaapi)
            {
                switch (_vaapiDriver)
                {
                    case VaapiDriver.i965:
                        startInfo.EnvironmentVariables.Add("LIBVA_DRIVER_NAME", "i965");
                        break;
                    case VaapiDriver.iHD:
                        startInfo.EnvironmentVariables.Add("LIBVA_DRIVER_NAME", "iHD");
                        break;
                }
            }

            if (_saveReports)
            {
                string fileName = _isConcat
                    ? Path.Combine(FileSystemLayout.FFmpegReportsFolder, "ffmpeg-%t-concat.log")
                    : Path.Combine(FileSystemLayout.FFmpegReportsFolder, "ffmpeg-%t-transcode.log");
                startInfo.EnvironmentVariables.Add("FFREPORT", $"file={fileName}:level=32");
            }

            startInfo.ArgumentList.Add("-nostdin");
            foreach (string argument in _arguments)
            {
                startInfo.ArgumentList.Add(argument);
            }

            return new Process
            {
                StartInfo = startInfo
            };
        }
    }
}
