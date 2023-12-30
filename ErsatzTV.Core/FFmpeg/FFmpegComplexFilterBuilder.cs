using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.FFmpeg;
using ErsatzTV.FFmpeg.State;

namespace ErsatzTV.Core.FFmpeg;

public class FFmpegComplexFilterBuilder
{
    private bool _boxBlur;
    private Option<List<FadePoint>> _maybeFadePoints = None;
    private Option<IDisplaySize> _padToSize = None;
    private string _pixelFormat;
    private IDisplaySize _resolution;
    private Option<IDisplaySize> _scaleToSize = None;
    private Option<string> _subtitle;
    private Option<ChannelWatermark> _watermark;
    private Option<int> _watermarkIndex;

    public FFmpegComplexFilterBuilder WithScaling(IDisplaySize scaleToSize)
    {
        _scaleToSize = Some(scaleToSize);
        return this;
    }

    public FFmpegComplexFilterBuilder WithBlackBars(IDisplaySize padToSize)
    {
        _padToSize = Some(padToSize);
        return this;
    }

    public FFmpegComplexFilterBuilder WithInputPixelFormat(Option<string> maybePixelFormat)
    {
        foreach (string pixelFormat in maybePixelFormat)
        {
            _pixelFormat = pixelFormat;
        }

        return this;
    }

    public FFmpegComplexFilterBuilder WithWatermark(
        Option<ChannelWatermark> watermark,
        Option<List<FadePoint>> maybeFadePoints,
        IDisplaySize resolution,
        Option<int> watermarkIndex)
    {
        _watermark = watermark;
        _maybeFadePoints = maybeFadePoints;
        _resolution = resolution;
        _watermarkIndex = watermarkIndex;
        return this;
    }

    public FFmpegComplexFilterBuilder WithBoxBlur(bool boxBlur)
    {
        _boxBlur = boxBlur;
        return this;
    }

    public FFmpegComplexFilterBuilder WithSubtitleFile(Option<string> subtitleFile)
    {
        foreach (string file in subtitleFile)
        {
            string effectiveFile = file;
            string fontsDir = FileSystemLayout.ResourcesCacheFolder;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                fontsDir = fontsDir
                    .Replace(@"\", @"/\")
                    .Replace(@":/", @"\\:/");

                effectiveFile = effectiveFile
                    .Replace(@"\", @"/\")
                    .Replace(@":/", @"\\:/");
            }

            _subtitle = $"subtitles={effectiveFile}:fontsdir={fontsDir}";
        }

        return this;
    }

    public Option<FFmpegComplexFilter> Build(
        bool videoOnly,
        int videoInput,
        int videoStreamIndex,
        int audioInput,
        Option<int> audioStreamIndex,
        bool isSong)
    {
        // since .Contains is used on pixel format, we need it to be not null
        _pixelFormat ??= string.Empty;

        var complexFilter = new StringBuilder();

        var videoLabel = $"{videoInput}:{(isSong ? "v" : videoStreamIndex.ToString(CultureInfo.InvariantCulture))}";
        string audioLabel = audioStreamIndex.Match(index => $"{audioInput}:{index}", () => "0:a");

        var videoFilterQueue = new List<string>();
        var watermarkPreprocess = new List<string>();
        string watermarkOverlay = string.Empty;

        if (isSong)
        {
            videoFilterQueue.Add("format=yuv420p");
        }

        bool scaleOrPad = _scaleToSize.IsSome || _padToSize.IsSome;
        bool usesSoftwareFilters = _padToSize.IsSome || _watermark.IsSome;
        bool hasFadePoints = _maybeFadePoints.Map(fp => fp.Count).IfNone(0) > 0;

        var softwareFilterQueue = new List<string>();
        if (usesSoftwareFilters)
        {
            if (_boxBlur)
            {
                softwareFilterQueue.Add("boxblur=40");
            }

            if (videoOnly)
            {
                softwareFilterQueue.Add("deband");
            }

            foreach (ChannelWatermark watermark in _watermark)
            {
                Option<string> maybeFormats = watermark.Opacity != 100 || hasFadePoints
                    ? "yuva420p|yuva444p|yuva422p|rgba|abgr|bgra|gbrap|ya8"
                    : None;

                foreach (string formats in maybeFormats)
                {
                    watermarkPreprocess.Add($"format={formats}");
                }

                double horizontalMargin = Math.Round(watermark.HorizontalMarginPercent / 100.0 * _resolution.Width);
                double verticalMargin = Math.Round(watermark.VerticalMarginPercent / 100.0 * _resolution.Height);

                string position = watermark.Location switch
                {
                    WatermarkLocation.BottomLeft => $"x={horizontalMargin}:y=H-h-{verticalMargin}",
                    WatermarkLocation.TopLeft => $"x={horizontalMargin}:y={verticalMargin}",
                    WatermarkLocation.TopRight => $"x=W-w-{horizontalMargin}:y={verticalMargin}",
                    WatermarkLocation.TopMiddle => $"x=(W-w)/2:y={verticalMargin}",
                    WatermarkLocation.RightMiddle => $"x=W-w-{horizontalMargin}:y=(H-h)/2",
                    WatermarkLocation.BottomMiddle => $"x=(W-w)/2:y=H-h-{verticalMargin}",
                    WatermarkLocation.LeftMiddle => $"x={horizontalMargin}:y=(H-h)/2",
                    _ => $"x=W-w-{horizontalMargin}:y=H-h-{verticalMargin}"
                };

                if (watermark.Opacity != 100)
                {
                    double opacity = watermark.Opacity / 100.0;
                    watermarkPreprocess.Add($"colorchannelmixer=aa={opacity:F2}");
                }

                if (watermark.Size == WatermarkSize.Scaled)
                {
                    double width = Math.Round(watermark.WidthPercent / 100.0 * _resolution.Width);
                    watermarkPreprocess.Add($"scale={width}:-1");
                }

                foreach (List<FadePoint> fadePoints in _maybeFadePoints)
                {
                    watermarkPreprocess.AddRange(fadePoints.Map(fp => fp.ToFilter()));
                }

                watermarkOverlay = $"overlay={position}";

                if (hasFadePoints)
                {
                    watermarkOverlay += "," + isSong switch
                    {
                        true => "format=yuv420p",
                        false => "format=nv12"
                    };
                }
            }
        }

        string outputPixelFormat = null;

        _scaleToSize.IfSome(
            size =>
            {
                string filter = videoOnly switch
                {
                    true =>
                        $"scale={size.Width}:{size.Height}:force_original_aspect_ratio=increase,crop={size.Width}:{size.Height}",
                    false => $"scale={size.Width}:{size.Height}:flags=fast_bilinear"
                };

                if (!string.IsNullOrWhiteSpace(filter))
                {
                    videoFilterQueue.Add(filter);
                }
            });

        if (scaleOrPad && _boxBlur == false)
        {
            videoFilterQueue.Add("setsar=1");
        }

        videoFilterQueue.AddRange(softwareFilterQueue);

        _padToSize.IfSome(size => videoFilterQueue.Add($"pad={size.Width}:{size.Height}:(ow-iw)/2:(oh-ih)/2"));

        foreach (string subtitle in _subtitle)
        {
            videoFilterQueue.Add(subtitle);
        }

        if (videoFilterQueue.Count != 0 || !string.IsNullOrWhiteSpace(watermarkOverlay))
        {
            if (videoFilterQueue.Count != 0)
            {
                complexFilter.Append(CultureInfo.InvariantCulture, $"[{videoLabel}]");
                var filters = string.Join(",", videoFilterQueue);
                complexFilter.Append(filters);
            }

            if (!string.IsNullOrWhiteSpace(watermarkOverlay))
            {
                if (videoFilterQueue.Count != 0)
                {
                    complexFilter.Append("[vt];");
                }

                var watermarkLabel = $"[{audioInput + 1}:v]";
                foreach (int index in _watermarkIndex)
                {
                    watermarkLabel = $"[{audioInput + 1}:{index}]";
                }

                if (watermarkPreprocess.Count > 0)
                {
                    var joined = string.Join(",", watermarkPreprocess);
                    complexFilter.Append(CultureInfo.InvariantCulture, $"{watermarkLabel}{joined}[wmp];");
                    watermarkLabel = "[wmp]";
                }

                complexFilter.Append(
                    videoFilterQueue.Count != 0
                        ? $"[vt]{watermarkLabel}{watermarkOverlay}"
                        : $"[{videoLabel}]{watermarkLabel}{watermarkOverlay}");
            }

            videoLabel = "[v]";
            complexFilter.Append(videoLabel);
        }

        var filterResult = complexFilter.ToString();
        return string.IsNullOrWhiteSpace(filterResult)
            ? Option<FFmpegComplexFilter>.None
            : new FFmpegComplexFilter(filterResult, videoLabel, audioLabel, outputPixelFormat);
    }
}
