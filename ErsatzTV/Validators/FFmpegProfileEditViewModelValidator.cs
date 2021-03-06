using System.Collections.Generic;
using ErsatzTV.Core.Domain;
using ErsatzTV.ViewModels;
using FluentValidation;

namespace ErsatzTV.Validators
{
    public class FFmpegProfileEditViewModelValidator : AbstractValidator<FFmpegProfileEditViewModel>
    {
        private static readonly List<string> QsvEncoders = new() { "h264_qsv", "hevc_qsv", "mpeg2_qsv" };
        private static readonly List<string> NvencEncoders = new() { "h264_nvenc", "hevc_nvenc" };
        private static readonly List<string> VaapiEncoders = new() { "h264_vaapi", "hevc_vaapi", "mpeg2_vaapi" };

        public FFmpegProfileEditViewModelValidator()
        {
            RuleFor(x => x.Name).NotEmpty();
            RuleFor(x => x.ThreadCount).GreaterThan(0);

            When(
                x => x.Transcode,
                () =>
                {
                    RuleFor(x => x.VideoCodec).NotEmpty();
                    RuleFor(x => x.VideoBitrate).GreaterThan(0);
                    RuleFor(x => x.VideoBufferSize).GreaterThan(0);

                    RuleFor(x => x.AudioCodec).NotEmpty();
                    RuleFor(x => x.AudioBitrate).GreaterThan(0);
                    RuleFor(x => x.AudioVolume).GreaterThanOrEqualTo(0);
                    RuleFor(x => x.AudioChannels).GreaterThan(0);
                });

            When(
                x => x.HardwareAcceleration == HardwareAccelerationKind.Qsv,
                () =>
                {
                    RuleFor(x => x.VideoCodec).Must(c => QsvEncoders.Contains(c))
                        .WithMessage("QSV codec is required (h264_qsv, hevc_qsv, mpeg2_qsv)");
                });

            When(
                x => x.HardwareAcceleration == HardwareAccelerationKind.Nvenc,
                () =>
                {
                    RuleFor(x => x.VideoCodec).Must(c => NvencEncoders.Contains(c))
                        .WithMessage("NVENC codec is required (h264_nvenc, hevc_nvenc)");

                    // RuleFor(x => x.NormalizeResolution).Must(x => x == false)
                    //     .WithMessage("Resolution normalization (scaling) is not yet supported with NVENC");
                });
            
            When(
                x => x.HardwareAcceleration == HardwareAccelerationKind.Vaapi,
                () =>
                {
                    RuleFor(x => x.VideoCodec).Must(c => VaapiEncoders.Contains(c))
                        .WithMessage("VAAPI codec is required (h264_vaapi, hevc_vaapi, mpeg2_vaapi)");
                });

            When(
                x => x.HardwareAcceleration == HardwareAccelerationKind.None,
                () =>
                {
                    RuleFor(x => x.VideoCodec).Must(c => !QsvEncoders.Contains(c) && !NvencEncoders.Contains(c))
                        .WithMessage("Hardware acceleration is required for this codec");
                });
        }
    }
}
