using System;
using System.Collections.Generic;
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
        private string _pixelFormat;

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

        public FFmpegComplexFilterBuilder WithInputCodec(string codec)
        {
            _inputCodec = codec;
            return this;
        }

        public FFmpegComplexFilterBuilder WithInputPixelFormat(string pixelFormat)
        {
            _pixelFormat = pixelFormat;
            return this;
        }

        public FFmpegComplexFilterBuilder WithWatermark(Option<ChannelWatermark> watermark, IDisplaySize resolution)
        {
            _watermark = watermark;
            _resolution = resolution;
            return this;
        }

        public Option<FFmpegComplexFilter> Build(int videoStreamIndex, Option<int> audioStreamIndex)
        {
            var complexFilter = new StringBuilder();

            var videoLabel = $"0:{videoStreamIndex}";
            string audioLabel = audioStreamIndex.Match(index => $"0:{index}", () => "0:a");

            HardwareAccelerationKind acceleration = _hardwareAccelerationKind.IfNone(HardwareAccelerationKind.None);
            bool isHardwareDecode = acceleration switch
            {
                HardwareAccelerationKind.Vaapi => _inputCodec != "mpeg4",
                HardwareAccelerationKind.Nvenc => true,
                HardwareAccelerationKind.Qsv => true,
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
                audioDuration => audioFilterQueue.Add($"apad=whole_dur={audioDuration.TotalMilliseconds}ms"));

            bool usesHardwareFilters = acceleration != HardwareAccelerationKind.None && !isHardwareDecode &&
                                       (_deinterlace || _scaleToSize.IsSome);
            if (usesHardwareFilters)
            {
                videoFilterQueue.Add("hwupload");
            }

            if (_deinterlace)
            {
                string filter = acceleration switch
                {
                    HardwareAccelerationKind.Qsv => "deinterlace_qsv",
                    HardwareAccelerationKind.Nvenc => "", // TODO: yadif_cuda support in docker
                    HardwareAccelerationKind.Vaapi => "deinterlace_vaapi",
                    _ => "yadif=1"
                };

                if (!string.IsNullOrWhiteSpace(filter))
                {
                    videoFilterQueue.Add(filter);
                }
            }

            _scaleToSize.IfSome(
                size =>
                {
                    string filter = acceleration switch
                    {
                        HardwareAccelerationKind.Qsv => $"scale_qsv=w={size.Width}:h={size.Height}",
                        HardwareAccelerationKind.Nvenc when _pixelFormat == "yuv420p10le" =>
                            $"hwdownload,format=p010le,format=nv12,hwupload,scale_npp={size.Width}:{size.Height}",
                        HardwareAccelerationKind.Nvenc => $"hwupload_cuda,scale_npp={size.Width}:{size.Height}",
                        HardwareAccelerationKind.Vaapi => $"scale_vaapi=w={size.Width}:h={size.Height}",
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
                        HardwareAccelerationKind.Nvenc when _scaleToSize.IsNone && _pixelFormat == "yuv420p10le" =>
                            "format=p010le,format=nv12",
                        _ => "format=yuv420p|nv12"
                    };
                    videoFilterQueue.Add(format);
                }

                if (scaleOrPad)
                {
                    videoFilterQueue.Add("setsar=1");
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

            bool hasAudioFilters = audioFilterQueue.Any();
            if (hasAudioFilters)
            {
                complexFilter.Append($"[{audioLabel}]");
                complexFilter.Append(string.Join(",", audioFilterQueue));
                audioLabel = "[a]";
                complexFilter.Append(audioLabel);
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

                    var watermarkLabel = "[1:v]";
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
                        complexFilter.Append(",hwupload");
                    }
                }

                videoLabel = "[v]";
                complexFilter.Append(videoLabel);
            }

            var filterResult = complexFilter.ToString();
            return string.IsNullOrWhiteSpace(filterResult)
                ? Option<FFmpegComplexFilter>.None
                : new FFmpegComplexFilter(filterResult, videoLabel, audioLabel);
        }
    }
}
