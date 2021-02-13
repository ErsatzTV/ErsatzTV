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
using System.Linq;
using System.Text;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.FFmpeg;
using LanguageExt;

namespace ErsatzTV.Core.FFmpeg
{
    internal class FFmpegProcessBuilder
    {
        private readonly List<string> _arguments = new();
        private readonly Queue<string> _audioFilters = new();
        private readonly string _ffmpegPath;
        private readonly Queue<string> _videoFilters = new();

        public FFmpegProcessBuilder(string ffmpegPath) => _ffmpegPath = ffmpegPath;

        public FFmpegProcessBuilder WithThreads(int threads)
        {
            _arguments.Add("-threads");
            _arguments.Add($"{threads}");
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
            var arguments = new List<string>
            {
                "-f", "concat",
                "-safe", "0",
                "-protocol_whitelist", "file,http,tcp,https,tcp,tls",
                "-probesize", "32",
                "-i", concatPlaylist,
                "-map", "0:v",
                "-map", "0:a",
                "-c", "copy",
                "-muxdelay", "0",
                "-muxpreload", "0"
            };
            _arguments.AddRange(arguments);
            return this;
        }

        public FFmpegProcessBuilder WithMetadata(Channel channel)
        {
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

        public FFmpegProcessBuilder WithText(string text)
        {
            const string FONT_FILE = "fontfile=Resources/Roboto-Regular.ttf";
            const string FONT_SIZE = "fontsize=30";
            const string FONT_COLOR = "fontcolor=white";
            const string X = "x=(w-text_w)/2";
            const string Y = "y=(h-text_h)/2";

            return WithFiltergraph($"drawtext={FONT_FILE}:{FONT_SIZE}:{FONT_COLOR}:{X}:{Y}:text='{text}'");
        }

        public FFmpegProcessBuilder WithDuration(TimeSpan duration) =>
            // _arguments.Add("-t");
            // _arguments.Add($"{duration:c}");
            this;

        public FFmpegProcessBuilder WithFormat(string format)
        {
            _arguments.Add("-f");
            _arguments.Add($"{format}");
            return this;
        }

        public FFmpegProcessBuilder WithPlaybackArgs(FFmpegPlaybackSettings playbackSettings)
        {
            var arguments = new List<string>
            {
                "-c:v", playbackSettings.VideoCodec,
                "-flags", "cgop",
                "-sc_threshold", "1000000000"
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
                    "-map_metadata", "-1",
                    "-movflags", "+faststart",
                    "-muxdelay", "0",
                    "-muxpreload", "0"
                });

            _arguments.AddRange(arguments);
            return this;
        }

        public FFmpegProcessBuilder WithScaling(IDisplaySize displaySize, string algorithm)
        {
            _videoFilters.Enqueue($"scale={displaySize.Width}:{displaySize.Height}:flags={algorithm}");
            return this;
        }

        public FFmpegProcessBuilder WithBlackBars(IDisplaySize displaySize)
        {
            _videoFilters.Enqueue($"pad={displaySize.Width}:{displaySize.Height}:(ow-iw)/2:(oh-ih)/2");
            return this;
        }

        public FFmpegProcessBuilder WithAlignedAudio(Option<TimeSpan> audioDuration)
        {
            audioDuration.IfSome(duration => _audioFilters.Enqueue($"apad=whole_dur={duration.TotalMilliseconds}ms"));
            return this;
        }

        public FFmpegProcessBuilder WithDeinterlace(bool deinterlace, string algorithm = "yadif=1")
        {
            if (deinterlace)
            {
                _videoFilters.Enqueue(algorithm);
            }

            return this;
        }

        public FFmpegProcessBuilder WithSAR()
        {
            // TODO: minsiz?
            _videoFilters.Enqueue("setsar=1");
            return this;
        }

        public FFmpegProcessBuilder WithFilterComplex()
        {
            var complexFilter = new StringBuilder();
            var videoLabel = "0:v";
            var audioLabel = "0:a";
            bool hasVideoFilters = _videoFilters.Any();
            if (hasVideoFilters)
            {
                (string filter, string finalLabel) = GenerateVideoFilter(_videoFilters);
                complexFilter.Append(filter);
                videoLabel = finalLabel;
            }

            if (_audioFilters.Any())
            {
                if (hasVideoFilters)
                {
                    complexFilter.Append(';');
                }

                (string filter, string finalLabel) = GenerateAudioFilter(_audioFilters);
                complexFilter.Append(filter);
                audioLabel = finalLabel;
            }

            var complex = complexFilter.ToString();

            if (!string.IsNullOrWhiteSpace(complex))
            {
                _arguments.Add("-filter_complex");
                _arguments.Add(complex);
            }

            _arguments.Add("-map");
            _arguments.Add(videoLabel);

            _arguments.Add("-map");
            _arguments.Add(audioLabel);

            return this;
        }

        public FFmpegProcessBuilder WithQuiet()
        {
            _arguments.AddRange(new[] { "-hide_banner", "-loglevel", "panic", "-nostats" });
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

            foreach (string argument in _arguments)
            {
                startInfo.ArgumentList.Add(argument);
            }

            return new Process
            {
                StartInfo = startInfo
            };
        }

        private FilterResult GenerateVideoFilter(Queue<string> filterQueue) =>
            GenerateFilter(filterQueue, "null", 'v');

        private FilterResult GenerateAudioFilter(Queue<string> filterQueue) =>
            GenerateFilter(filterQueue, "anull", 'a');

        private static FilterResult GenerateFilter(Queue<string> filterQueue, string nullFilter, char av)
        {
            var filter = new StringBuilder();
            var index = 0;
            filter.Append($"[0:{av}]{nullFilter}[{av}{index}]");
            while (filterQueue.TryDequeue(out string result))
            {
                filter.Append($";[{av}{index}]{result}[{av}{++index}]");
            }

            return new FilterResult(filter.ToString(), $"[{av}{index}]");
        }

        private record FilterResult(string Filter, string FinalLabel);
    }
}
