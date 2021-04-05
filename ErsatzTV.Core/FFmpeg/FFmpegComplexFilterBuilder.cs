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
        private Option<string> _frameRate = None;
        private Option<HardwareAccelerationKind> _hardwareAccelerationKind = None;
        private string _inputCodec;
        private bool _normalizeLoudness;
        private Option<IDisplaySize> _padToSize = None;
        private Option<IDisplaySize> _scaleToSize = None;

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

        public FFmpegComplexFilterBuilder WithFrameRate(Option<string> frameRate)
        {
            _frameRate = frameRate;
            return this;
        }

        public Option<FFmpegComplexFilter> Build(int videoStreamIndex, int audioStreamIndex)
        {
            var complexFilter = new StringBuilder();

            var videoLabel = $"0:{videoStreamIndex}";
            var audioLabel = $"0:{audioStreamIndex}";

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

            _frameRate.IfSome(frameRate => videoFilterQueue.Add($"fps=fps={frameRate}"));

            _scaleToSize.IfSome(
                size =>
                {
                    string filter = acceleration switch
                    {
                        HardwareAccelerationKind.Qsv => $"scale_qsv=w={size.Width}:h={size.Height}",
                        HardwareAccelerationKind.Nvenc => $"scale_npp={size.Width}:{size.Height}",
                        HardwareAccelerationKind.Vaapi => $"scale_vaapi=w={size.Width}:h={size.Height}",
                        _ => $"scale={size.Width}:{size.Height}:flags=fast_bilinear"
                    };

                    if (!string.IsNullOrWhiteSpace(filter))
                    {
                        videoFilterQueue.Add(filter);
                    }
                });

            if (_scaleToSize.IsSome || _padToSize.IsSome)
            {
                if (acceleration != HardwareAccelerationKind.None && (isHardwareDecode || usesHardwareFilters))
                {
                    videoFilterQueue.Add("hwdownload");
                    string format = acceleration switch
                    {
                        HardwareAccelerationKind.Vaapi => "format=nv12|vaapi",
                        _ => "format=nv12"
                    };
                    videoFilterQueue.Add(format);
                }

                videoFilterQueue.Add("setsar=1");
            }

            _padToSize.IfSome(size => videoFilterQueue.Add($"pad={size.Width}:{size.Height}:(ow-iw)/2:(oh-ih)/2"));

            if ((_scaleToSize.IsSome || _padToSize.IsSome) && acceleration != HardwareAccelerationKind.None)
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

            if (videoFilterQueue.Any())
            {
                if (hasAudioFilters)
                {
                    complexFilter.Append(';');
                }

                complexFilter.Append($"[{videoLabel}]");
                complexFilter.Append(string.Join(",", videoFilterQueue));
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
