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
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.FFmpeg;
using LanguageExt;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Core.FFmpeg
{
    internal class FFmpegProcessBuilder
    {
        private readonly List<string> _arguments = new();
        private readonly string _ffmpegPath;
        private readonly bool _saveReports;
        private readonly ILogger _logger;
        private FFmpegComplexFilterBuilder _complexFilterBuilder = new();
        private bool _isConcat;
        private VaapiDriver _vaapiDriver;
        private string _vaapiDevice;
        private HardwareAccelerationKind _hwAccel;
        private string _outputPixelFormat;
        private bool _noAutoScale;
        private Option<int> _outputFramerate;

        public FFmpegProcessBuilder(string ffmpegPath, bool saveReports, ILogger logger)
        {
            _ffmpegPath = ffmpegPath;
            _saveReports = saveReports;
            _logger = logger;
        }

        public FFmpegProcessBuilder WithVaapiDriver(VaapiDriver vaapiDriver, string vaapiDevice)
        {
            if (vaapiDriver != VaapiDriver.Default)
            {
                _vaapiDriver = vaapiDriver;
            }

            _vaapiDevice = string.IsNullOrWhiteSpace(vaapiDevice)
                ? "/dev/dri/renderD128"
                : vaapiDevice;

            return this;
        }

        public FFmpegProcessBuilder WithThreads(int threads)
        {
            _arguments.Add("-threads");
            _arguments.Add($"{threads}");
            return this;
        }

        public FFmpegProcessBuilder WithHardwareAcceleration(HardwareAccelerationKind hwAccel, Option<string> pixelFormat, string encoder)
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
                    string outputFormat = (encoder, pixelFormat.IfNone("")) switch
                    {
                        ("hevc_nvenc", "yuv420p10le") => "p010le",
                        ("h264_nvenc", "yuv420p10le") => "p010le",
                        // ("hevc_nvenc", "yuv444p10le") => "p016le",
                        _ => "cuda"
                    };
                    
                    _arguments.Add("-hwaccel");
                    _arguments.Add("cuda");
                    _arguments.Add("-hwaccel_output_format");
                    _arguments.Add(outputFormat);
                    break;
                case HardwareAccelerationKind.Vaapi:
                    _arguments.Add("-hwaccel");
                    _arguments.Add("vaapi");
                    _arguments.Add("-vaapi_device");
                    _arguments.Add(_vaapiDevice);
                    _arguments.Add("-hwaccel_output_format");
                    _arguments.Add("vaapi");
                    break;
                case HardwareAccelerationKind.VideoToolbox:
                    _arguments.Add("-hwaccel");
                    _arguments.Add("videotoolbox");
                    break;
            }

            _complexFilterBuilder = _complexFilterBuilder.WithHardwareAcceleration(hwAccel);

            return this;
        }

        public FFmpegProcessBuilder WithRealtimeOutput(bool realtimeOutput)
        {
            if (realtimeOutput)
            {
                if (!_arguments.Contains("-re"))
                {
                    _arguments.Add("-re");
                }
            }
            else
            {
                _arguments.RemoveAll(s => s == "-re");
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

        public FFmpegProcessBuilder WithInfiniteLoop(bool loop = true)
        {
            if (loop)
            {
                _arguments.Add("-stream_loop");
                _arguments.Add("-1");
                
                if (_hwAccel is HardwareAccelerationKind.Qsv or HardwareAccelerationKind.Vaapi)
                {
                    _noAutoScale = true;
                }
            }

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
            _arguments.Add(input);
            return this;
        }

        public FFmpegProcessBuilder WithMap(string map)
        {
            _arguments.Add("-map");
            _arguments.Add(map);
            return this;
        }

        public FFmpegProcessBuilder WithCopyCodec()
        {
            _arguments.Add("-c");
            _arguments.Add("copy");
            return this;
        }

        public FFmpegProcessBuilder WithWatermark(
            Option<WatermarkOptions> watermarkOptions,
            IDisplaySize resolution)
        {
            foreach (WatermarkOptions options in watermarkOptions)
            {
                foreach (string path in options.ImagePath)
                {
                    if (options.IsAnimated)
                    {
                        _arguments.Add("-ignore_loop");
                        _arguments.Add("0");
                    }

                    _arguments.Add("-i");
                    _arguments.Add(path);

                    _complexFilterBuilder = _complexFilterBuilder.WithWatermark(
                        options.Watermark,
                        resolution,
                        options.ImageStreamIndex);
                }
            }
            
            return this;
        }

        public FFmpegProcessBuilder WithSubtitleFile(Option<string> subtitleFile)
        {
            _complexFilterBuilder = _complexFilterBuilder.WithSubtitleFile(subtitleFile);
            return this;
        }

        public FFmpegProcessBuilder WithInputCodec(
            Option<TimeSpan> maybeStart,
            bool loop,
            string videoPath,
            string audioPath,
            string decoder,
            Option<string> codec,
            Option<string> pixelFormat)
        {
            if (audioPath == videoPath)
            {
                WithSeek(maybeStart);
                WithInfiniteLoop(loop);
            }
            else
            {
                _noAutoScale = true;
                _outputFramerate = 30;
                
                _arguments.Add("-loop");
                _arguments.Add("1");
            }

            if (!string.IsNullOrWhiteSpace(decoder))
            {
                _arguments.Add("-c:v");
                _arguments.Add(decoder);
            }

            _complexFilterBuilder = _complexFilterBuilder
                .WithInputCodec(codec)
                .WithInputPixelFormat(pixelFormat);

            _arguments.Add("-i");
            _arguments.Add(videoPath);

            if (audioPath != videoPath)
            {
                WithSeek(maybeStart);

                _arguments.Add("-i");
                _arguments.Add(audioPath);
            }

            return this;
        }
        
        public FFmpegProcessBuilder WithSongInput(
            string videoPath,
            Option<string> codec,
            Option<string> pixelFormat,
            bool boxBlur)
        {
            _noAutoScale = true;
            _outputFramerate = 30;

            _complexFilterBuilder = _complexFilterBuilder
                .WithInputCodec(codec)
                .WithInputPixelFormat(pixelFormat)
                .WithBoxBlur(boxBlur);

            _arguments.Add("-i");
            _arguments.Add(videoPath);

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

        public FFmpegProcessBuilder WithInitialDiscontinuity()
        {
            _arguments.Add("-mpegts_flags");
            _arguments.Add("+initial_discontinuity");
            return this;
        }

        public FFmpegProcessBuilder WithHls(string channelNumber, Option<MediaVersion> mediaVersion, long ptsOffset, Option<int> maybeTimeScale)
        {
            const int SEGMENT_SECONDS = 4;

            var frameRate = 24;

            foreach (MediaVersion version in mediaVersion)
            {
                if (!int.TryParse(version.RFrameRate, out int fr))
                {
                    string[] split = (version.RFrameRate ?? string.Empty).Split("/");
                    if (int.TryParse(split[0], out int left) && int.TryParse(split[1], out int right))
                    {
                        fr = (int)Math.Round(left / (double)right);
                    }
                    else
                    {
                        _logger.LogInformation("Unable to detect framerate, using {FrameRate}", 24);
                        fr = 24;
                    }
                }

                frameRate = fr;
            }

            foreach (int timescale in maybeTimeScale)
            {
                _arguments.Add("-output_ts_offset");
                _arguments.Add($"{(ptsOffset / (double)timescale).ToString(NumberFormatInfo.InvariantInfo)}");
            }

            _arguments.AddRange(
                new[]
                {
                    "-g", $"{frameRate * SEGMENT_SECONDS}",
                    "-keyint_min", $"{frameRate * SEGMENT_SECONDS}",
                    "-force_key_frames", $"expr:gte(t,n_forced*{SEGMENT_SECONDS})",
                    "-f", "hls",
                    "-hls_time", $"{SEGMENT_SECONDS}",
                    "-hls_list_size", "0",
                    "-segment_list_flags", "+live",
                    "-hls_segment_filename",
                    Path.Combine(FileSystemLayout.TranscodeFolder, channelNumber, "live%06d.ts"),
                    "-hls_flags", "program_date_time+append_list+discont_start+omit_endlist+independent_segments",
                    "-mpegts_flags", "+initial_discontinuity",
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
                // disable scene change detection except with mpeg2video
                "-sc_threshold", playbackSettings.VideoCodec == "mpeg2video" ? "1000000000" : "0"
            };

            if (!string.IsNullOrWhiteSpace(_outputPixelFormat))
            {
                arguments.AddRange(new[] { "-pix_fmt", _outputPixelFormat });
            }

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

            if (_noAutoScale)
            {
                _arguments.Add("-noautoscale");
            }

            foreach (int framerate in _outputFramerate)
            {
                _arguments.Add("-r");
                _arguments.Add(framerate.ToString());
            }

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
        
        public FFmpegProcessBuilder WithOutputFormat(string format, string output)
        {
            _arguments.Add("-f");
            _arguments.Add(format);
            
            _arguments.Add("-y");
            _arguments.Add(output);

            return this;
        }

        public FFmpegProcessBuilder WithFilterComplex(
            MediaStream videoStream,
            Option<MediaStream> maybeAudioStream,
            string videoPath,
            Option<string> audioPath,
            string videoCodec)
        {
            _complexFilterBuilder = _complexFilterBuilder.WithVideoEncoder(videoCodec);

            int videoStreamIndex = videoStream.Index;
            Option<int> maybeIndex = maybeAudioStream.Map(ms => ms.Index);

            var videoIndex = 0;
            var audioIndex = 0;
            if (audioPath.IsNone)
            {
                // no audio index, so use same as video
                audioIndex = 0;
            }
            else if (audioPath.IfNone("NotARealPath") != videoPath)
            {
                audioIndex = 1;
                if (_hwAccel == HardwareAccelerationKind.None)
                {
                    _outputPixelFormat = "yuv420p";
                }
            }

            var videoLabel = $"{videoIndex}:{videoStreamIndex}";
            var audioLabel = $"{audioIndex}:{maybeIndex.Match(i => i.ToString(), () => "a")}";

            Option<FFmpegComplexFilter> maybeFilter = _complexFilterBuilder.Build(
                audioPath.IsNone,
                videoIndex,
                videoStreamIndex,
                audioIndex,
                maybeIndex,
                audioPath.IsSome && videoPath != audioPath.IfNone("NotARealPath"));

            maybeFilter.IfSome(
                filter =>
                {
                    _arguments.Add("-filter_complex");
                    _arguments.Add(filter.ComplexFilter);
                    videoLabel = filter.VideoLabel;
                    audioLabel = filter.AudioLabel;

                    if (!string.IsNullOrWhiteSpace(filter.PixelFormat))
                    {
                        _outputPixelFormat = filter.PixelFormat;
                    }
                });

            _arguments.Add("-map");
            _arguments.Add(videoLabel);

            foreach (string _ in audioPath)
            {
                _arguments.Add("-map");
                _arguments.Add(audioLabel);
            }

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
                        startInfo.EnvironmentVariables["LIBVA_DRIVER_NAME"] = "i965";
                        break;
                    case VaapiDriver.iHD:
                        startInfo.EnvironmentVariables["LIBVA_DRIVER_NAME"] = "iHD";
                        break;
                    case VaapiDriver.RadeonSI:
                        startInfo.EnvironmentVariables["LIBVA_DRIVER_NAME"] = "radeonsi";
                        break;
                }
            }

            if (_saveReports)
            {
                string fileName = _isConcat
                    ? Path.Combine(FileSystemLayout.FFmpegReportsFolder, "ffmpeg-%t-concat.log")
                    : Path.Combine(FileSystemLayout.FFmpegReportsFolder, "ffmpeg-%t-transcode.log");

                // rework filename in a format that works on windows
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    // \ is escape, so use / for directory separators
                    fileName = fileName.Replace(@"\", @"/");
                    
                    // colon after drive letter needs to be escaped
                    fileName = fileName.Replace(@":/", @"\:/");
                }

                startInfo.EnvironmentVariables["FFREPORT"] = $"file={fileName}:level=32";
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
