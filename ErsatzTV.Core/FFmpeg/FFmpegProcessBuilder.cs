// zlib License
//
// Copyright (c) 2022 Dan Ferguson, Victor Hugo Soliz Kuncar, Jason Dove
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

using System.Diagnostics;
using System.Text;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.FFmpeg;

namespace ErsatzTV.Core.FFmpeg;

internal class FFmpegProcessBuilder
{
    private readonly List<string> _arguments = new();
    private readonly string _ffmpegPath;
    private FFmpegComplexFilterBuilder _complexFilterBuilder = new();

    public FFmpegProcessBuilder(string ffmpegPath) => _ffmpegPath = ffmpegPath;

    public FFmpegProcessBuilder WithThreads(int threads)
    {
        _arguments.Add("-threads");
        _arguments.Add($"{threads}");
        return this;
    }

    public FFmpegProcessBuilder WithWatermark(
        Option<WatermarkOptions> watermarkOptions,
        Option<List<FadePoint>> maybeFadePoints,
        IDisplaySize resolution)
    {
        ChannelWatermarkMode maybeWatermarkMode = watermarkOptions.Map(wmo => wmo.Watermark.Map(wm => wm.Mode))
            .Flatten()
            .IfNone(ChannelWatermarkMode.None);

        // skip watermark if intermittent and no fade points
        if (maybeWatermarkMode != ChannelWatermarkMode.None &&
            (maybeWatermarkMode != ChannelWatermarkMode.Intermittent ||
             maybeFadePoints.Map(fp => fp.Count > 0).IfNone(false)))
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

                    // when we have fade points, we need to loop the static watermark image
                    else if (maybeFadePoints.Map(fp => fp.Count).IfNone(0) > 0)
                    {
                        _arguments.Add("-stream_loop");
                        _arguments.Add("-1");
                    }

                    _arguments.Add("-i");
                    _arguments.Add(path);

                    _complexFilterBuilder = _complexFilterBuilder.WithWatermark(
                        options.Watermark,
                        maybeFadePoints,
                        resolution,
                        options.ImageStreamIndex);
                }
            }
        }

        return this;
    }

    public FFmpegProcessBuilder WithSubtitleFile(Option<string> subtitleFile)
    {
        _complexFilterBuilder = _complexFilterBuilder.WithSubtitleFile(subtitleFile);
        return this;
    }

    public FFmpegProcessBuilder WithSongInput(
        string videoPath,
        Option<string> pixelFormat,
        bool boxBlur)
    {
        _complexFilterBuilder = _complexFilterBuilder
            .WithInputPixelFormat(pixelFormat)
            .WithBoxBlur(boxBlur);

        _arguments.Add("-i");
        _arguments.Add(videoPath);

        return this;
    }

    public FFmpegProcessBuilder WithFormatFlags(IEnumerable<string> formatFlags)
    {
        _arguments.Add("-fflags");
        _arguments.Add(string.Join(string.Empty, formatFlags));
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

    public FFmpegProcessBuilder WithOutputFormat(string format, string output, params string[] options)
    {
        foreach (string option in options)
        {
            _arguments.Add(option);
        }

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
        Option<string> audioPath)
    {
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
            });

        foreach (string _ in audioPath)
        {
            _arguments.Add("-map");
            _arguments.Add(audioLabel);
        }

        _arguments.Add("-map");
        _arguments.Add(videoLabel);

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
