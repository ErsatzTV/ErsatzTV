using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.FFmpeg;
using LanguageExt;
using static LanguageExt.Prelude;

namespace ErsatzTV.Core.FFmpeg
{
    public class FFmpegComplexFilterBuilder
    {
        private Option<TimeSpan> _audioDuration = None;
        private bool _deinterlace;
        private Option<HardwareAccelerationKind> _hardwareAccelerationKind = None;
        private string _inputCodec;
        private bool _normalizeLoudness;
        private Option<IDisplaySize> _padToSize = None;
        private IDisplaySize _resolution;
        private Option<IDisplaySize> _scaleToSize = None;
        private Option<ChannelWatermark> _watermark;
        private Option<int> _watermarkIndex;
        private string _pixelFormat;
        private string _videoEncoder;
        private Option<string> _drawtext;

        public FFmpegComplexFilterBuilder WithHardwareAcceleration(HardwareAccelerationKind hardwareAccelerationKind)
        {
            _hardwareAccelerationKind = Some(hardwareAccelerationKind);
            return this;
        }

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

        public FFmpegComplexFilterBuilder WithDeinterlace(bool deinterlace)
        {
            _deinterlace = deinterlace;
            return this;
        }

        public FFmpegComplexFilterBuilder WithAlignedAudio(Option<TimeSpan> audioDuration)
        {
            _audioDuration = audioDuration;
            return this;
        }

        public FFmpegComplexFilterBuilder WithNormalizeLoudness(bool normalizeLoudness)
        {
            _normalizeLoudness = normalizeLoudness;
            return this;
        }

        public FFmpegComplexFilterBuilder WithInputCodec(Option<string> maybeCodec)
        {
            foreach (string codec in maybeCodec)
            {
                _inputCodec = codec;
            }

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
            IDisplaySize resolution,
            Option<int> watermarkIndex)
        {
            _watermark = watermark;
            _resolution = resolution;
            _watermarkIndex = watermarkIndex;
            return this;
        }
        
        public FFmpegComplexFilterBuilder WithDrawtextFile(
            MediaVersion videoVersion,
            Option<string> drawtextFile)
        {
            foreach (string file in drawtextFile)
            {
                if (videoVersion is FallbackMediaVersion or CoverArtMediaVersion)
                {
                    string fontPath = Path.Combine(FileSystemLayout.ResourcesCacheFolder, "OPTIKabel-Heavy.otf");

                    // TODO: calculate by percent
                    _drawtext =
                        $"drawtext=fontfile={fontPath}:textfile={file}:x=50:y=H-175:fontsize=36:fontcolor=white";
                }
            }

            return this;
        }

        public FFmpegComplexFilterBuilder WithVideoEncoder(string videoEncoder)
        {
            _videoEncoder = videoEncoder;
            return this;
        }

        public Option<FFmpegComplexFilter> Build(string videoPath, int videoInput, int videoStreamIndex, int audioInput, Option<int> audioStreamIndex, bool isSong)
        {
            var complexFilter = new StringBuilder();

            var videoLabel = $"{videoInput}:{(isSong ? "v" : videoStreamIndex.ToString())}";
            string audioLabel = audioStreamIndex.Match(index => $"{audioInput}:{index}", () => "0:a");

            HardwareAccelerationKind acceleration = _hardwareAccelerationKind.IfNone(HardwareAccelerationKind.None);
            bool isHardwareDecode = acceleration switch
            {
                HardwareAccelerationKind.Vaapi => (!isSong || !videoPath.EndsWith(".png")) && _inputCodec != "mpeg4",
                HardwareAccelerationKind.Nvenc => !isSong || !videoPath.EndsWith(".png"),
                HardwareAccelerationKind.Qsv => !isSong || !videoPath.EndsWith(".png"),
                _ => false
            };

            var audioFilterQueue = new List<string>();
            var videoFilterQueue = new List<string>();
            string watermarkPreprocess = string.Empty;
            string watermarkOverlay = string.Empty;
            
            if (_normalizeLoudness)
            {
                audioFilterQueue.Add("loudnorm=I=-16:TP=-1.5:LRA=11");
            }

            _audioDuration.IfSome(
                audioDuration =>
                {
                    var durationString = audioDuration.TotalMilliseconds.ToString(NumberFormatInfo.InvariantInfo);
                    audioFilterQueue.Add($"apad=whole_dur={durationString}ms");
                });

            bool usesHardwareFilters = acceleration != HardwareAccelerationKind.None && !isHardwareDecode &&
                                       (_deinterlace || _scaleToSize.IsSome);

            if (isSong && !isHardwareDecode)
            {
                videoFilterQueue.Add("format=yuv420p");
            }

            switch (usesHardwareFilters, isSong && isHardwareDecode,  acceleration)
            {
                case (true, false, HardwareAccelerationKind.Nvenc):
                    videoFilterQueue.Add("hwupload_cuda");
                    break;
                case (true, false, _):
                    videoFilterQueue.Add("hwupload");
                    break;
            }

            if (_deinterlace)
            {
                string filter = acceleration switch
                {
                    HardwareAccelerationKind.Qsv => "deinterlace_qsv",
                    HardwareAccelerationKind.Nvenc => "yadif_cuda",
                    HardwareAccelerationKind.Vaapi => "deinterlace_vaapi",
                    _ => "yadif=1"
                };

                if (!string.IsNullOrWhiteSpace(filter))
                {
                    videoFilterQueue.Add(filter);
                }
            }

            string[] h264hevc = { "h264", "hevc" };

            if (acceleration == HardwareAccelerationKind.Vaapi && (_pixelFormat ?? string.Empty).EndsWith("p10le") &&
                h264hevc.Contains(_inputCodec)
                && (_pixelFormat != "yuv420p10le" || _inputCodec != "hevc"))
            {
                videoFilterQueue.Add("format=p010le,format=nv12|vaapi,hwupload");
            }

            if (acceleration == HardwareAccelerationKind.Vaapi && _pixelFormat == "yuv444p" && h264hevc.Contains(_inputCodec))
            {
                videoFilterQueue.Add("format=nv12|vaapi,hwupload");
            }

            _scaleToSize.IfSome(
                size =>
                {
                    string filter = acceleration switch
                    {
                        HardwareAccelerationKind.Qsv => $"scale_qsv=w={size.Width}:h={size.Height}",
                        HardwareAccelerationKind.Nvenc when _pixelFormat is "yuv420p10le" =>
                            $"hwupload_cuda,scale_cuda={size.Width}:{size.Height}",
                        HardwareAccelerationKind.Nvenc when isSong => $"scale_cuda={size.Width}:{size.Height}:format=yuv420p",
                        HardwareAccelerationKind.Nvenc => $"scale_cuda={size.Width}:{size.Height}",
                        HardwareAccelerationKind.Vaapi => $"scale_vaapi=format=nv12:w={size.Width}:h={size.Height}",
                        _ => $"scale={size.Width}:{size.Height}:flags=fast_bilinear"
                    };

                    if (!string.IsNullOrWhiteSpace(filter))
                    {
                        videoFilterQueue.Add(filter);
                    }
                });

            bool scaleOrPad = _scaleToSize.IsSome || _padToSize.IsSome;
            bool usesSoftwareFilters = scaleOrPad || _watermark.IsSome;

            if (usesSoftwareFilters)
            {
                if (acceleration != HardwareAccelerationKind.None && (isHardwareDecode || usesHardwareFilters))
                {
                    videoFilterQueue.Add("hwdownload");
                    string format = acceleration switch
                    {
                        HardwareAccelerationKind.Vaapi => "format=nv12|vaapi",
                        HardwareAccelerationKind.Nvenc when _pixelFormat == "yuv420p10le" =>
                            "format=p010le,format=nv12",
                        _ when isSong => "format=yuv420p",
                        _ => "format=nv12"
                    };
                    videoFilterQueue.Add(format);
                }

                if (scaleOrPad)
                {
                    videoFilterQueue.Add("setsar=1");
                }

                if (isSong)
                {
                    videoFilterQueue.Add("boxblur=75,fps=24");
                }

                foreach (ChannelWatermark watermark in _watermark)
                {
                    string enable = watermark.Mode == ChannelWatermarkMode.Intermittent
                        ? $":enable='lt(mod(mod(time(0),60*60),{watermark.FrequencyMinutes}*60),{watermark.DurationSeconds})'"
                        : string.Empty;

                    double horizontalMargin = Math.Round(watermark.HorizontalMarginPercent / 100.0 * _resolution.Width);
                    double verticalMargin = Math.Round(watermark.VerticalMarginPercent / 100.0 * _resolution.Height);

                    string position = watermark.Location switch
                    {
                        ChannelWatermarkLocation.BottomLeft => $"x={horizontalMargin}:y=H-h-{verticalMargin}",
                        ChannelWatermarkLocation.TopLeft => $"x={horizontalMargin}:y={verticalMargin}",
                        ChannelWatermarkLocation.TopRight => $"x=W-w-{horizontalMargin}:y={verticalMargin}",
                        ChannelWatermarkLocation.TopMiddle => $"x=(W-w)/2:y={verticalMargin}",
                        ChannelWatermarkLocation.RightMiddle => $"x=W-w-{horizontalMargin}:y=(H-h)/2",
                        ChannelWatermarkLocation.BottomMiddle => $"x=(W-w)/2:y=H-h-{verticalMargin}",
                        ChannelWatermarkLocation.LeftMiddle => $"x={horizontalMargin}:y=(H-h)/2",
                        _ => $"x=W-w-{horizontalMargin}:y=H-h-{verticalMargin}"
                    };

                    if (watermark.Size == ChannelWatermarkSize.Scaled)
                    {
                        double width = Math.Round(watermark.WidthPercent / 100.0 * _resolution.Width);
                        watermarkPreprocess = $"scale={width}:-1";
                    }
                    
                    if (watermark.Opacity != 100)
                    {
                        const string FORMATS = "yuva420p|yuva444p|yuva422p|rgba|abgr|bgra|gbrap|ya8"; 
                        string join = string.Empty;
                        double opacity = watermark.Opacity / 100.0;
                        if (!string.IsNullOrWhiteSpace(watermarkPreprocess))
                        {
                            join = ",";
                        }

                        watermarkPreprocess = $"format={FORMATS},colorchannelmixer=aa={opacity:F2}{join}{watermarkPreprocess}";
                    }

                    watermarkOverlay = $"overlay={position}{enable}";
                }
            }

            _padToSize.IfSome(size => videoFilterQueue.Add($"pad={size.Width}:{size.Height}:(ow-iw)/2:(oh-ih)/2"));
            
            foreach (string drawtext in _drawtext)
            {
                videoFilterQueue.Add(drawtext);
            }

            string outputPixelFormat = null;
            
            if (usesSoftwareFilters && acceleration != HardwareAccelerationKind.None &&
                string.IsNullOrWhiteSpace(watermarkOverlay))
            {
                string upload = acceleration switch
                {
                    HardwareAccelerationKind.Qsv => "hwupload=extra_hw_frames=64",
                    _ => "hwupload"
                };
                videoFilterQueue.Add(upload);
            }

            if (!usesSoftwareFilters && string.IsNullOrWhiteSpace(watermarkOverlay))
            {
                switch (acceleration, _videoEncoder, _pixelFormat)
                {
                    case (HardwareAccelerationKind.Nvenc, "h264_nvenc", "yuv420p10le"):
                        outputPixelFormat = "yuv420p";
                        break;
                    case (HardwareAccelerationKind.Nvenc, "h264_nvenc", "yuv444p10le"):
                        outputPixelFormat = "yuv444p";
                        break;
                }
            }

            bool hasAudioFilters = audioFilterQueue.Any();
            if (hasAudioFilters)
            {
                complexFilter.Append($"[{audioLabel}]");
                complexFilter.Append(string.Join(",", audioFilterQueue));
                audioLabel = "[a]";
                complexFilter.Append(audioLabel);
            }

            // vaapi downsample 10bit hevc to 8bit h264
            if (acceleration == HardwareAccelerationKind.Vaapi && !videoFilterQueue.Any() &&
                _pixelFormat == "yuv420p10le" && _videoEncoder.StartsWith("h264"))
            {
                videoFilterQueue.Add("scale_vaapi=format=nv12");
            }

            if (videoFilterQueue.Any() || !string.IsNullOrWhiteSpace(watermarkOverlay))
            {
                if (hasAudioFilters)
                {
                    complexFilter.Append(';');
                }

                if (videoFilterQueue.Any())
                {
                    complexFilter.Append($"[{videoLabel}]");
                    var filters = string.Join(",", videoFilterQueue);
                    complexFilter.Append(filters);
                }

                if (!string.IsNullOrWhiteSpace(watermarkOverlay))
                {
                    if (videoFilterQueue.Any())
                    {
                        complexFilter.Append("[vt];");
                    }

                    var watermarkLabel = $"[{audioInput+1}:v]";
                    foreach (int index in _watermarkIndex)
                    {
                        watermarkLabel = $"[{audioInput+1}:{index}]";
                    }

                    if (!string.IsNullOrWhiteSpace(watermarkPreprocess))
                    {
                        complexFilter.Append($"{watermarkLabel}{watermarkPreprocess}[wmp];");
                        watermarkLabel = "[wmp]";
                    }

                    complexFilter.Append(
                        videoFilterQueue.Any()
                            ? $"[vt]{watermarkLabel}{watermarkOverlay}"
                            : $"[{videoLabel}]{watermarkLabel}{watermarkOverlay}");

                    if (usesSoftwareFilters && acceleration != HardwareAccelerationKind.None)
                    {
                        switch (isSong, acceleration)
                        {
                            case (true, HardwareAccelerationKind.Nvenc):
                                complexFilter.Append(",hwupload_cuda");
                                break;
                            default:
                                complexFilter.Append(",hwupload");
                                break;
                        }
                    }
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
}
