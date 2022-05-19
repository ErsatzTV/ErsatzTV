using ErsatzTV.Core.Domain;
using ErsatzTV.ViewModels;
using FluentValidation;

namespace ErsatzTV.Validators;

public class FFmpegProfileEditViewModelValidator : AbstractValidator<FFmpegProfileEditViewModel>
{
    private static readonly List<FFmpegProfileVideoFormat> QsvFormats = new()
    {
        FFmpegProfileVideoFormat.H264,
        FFmpegProfileVideoFormat.Hevc,
        FFmpegProfileVideoFormat.Mpeg2Video
    };

    private static readonly List<FFmpegProfileVideoFormat> NvencFormats = new()
    {
        FFmpegProfileVideoFormat.H264,
        FFmpegProfileVideoFormat.Hevc
    };

    private static readonly List<FFmpegProfileVideoFormat> VaapiFormats = new()
    {
        FFmpegProfileVideoFormat.H264,
        FFmpegProfileVideoFormat.Hevc,
        FFmpegProfileVideoFormat.Mpeg2Video
    };

    private static readonly List<FFmpegProfileVideoFormat> VideoToolboxFormats = new()
    {
        FFmpegProfileVideoFormat.H264,
        FFmpegProfileVideoFormat.Hevc
    };

    public FFmpegProfileEditViewModelValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.ThreadCount).GreaterThanOrEqualTo(0);

        RuleFor(x => x.VideoFormat).NotEqual(FFmpegProfileVideoFormat.None);
        RuleFor(x => x.VideoBitrate).GreaterThan(0);
        RuleFor(x => x.VideoBufferSize).GreaterThan(0);

        RuleFor(x => x.AudioFormat).NotEqual(FFmpegProfileAudioFormat.None);
        RuleFor(x => x.AudioBitrate).GreaterThan(0);
        RuleFor(x => x.AudioChannels).GreaterThan(0);

        When(
            x => x.HardwareAcceleration == HardwareAccelerationKind.Qsv,
            () =>
            {
                RuleFor(x => x.VideoFormat).Must(c => QsvFormats.Contains(c))
                    .WithMessage("QSV supports formats (h264, hevc, mpeg2video)");
            });

        When(
            x => x.HardwareAcceleration == HardwareAccelerationKind.Nvenc,
            () =>
            {
                RuleFor(x => x.VideoFormat).Must(c => NvencFormats.Contains(c))
                    .WithMessage("NVENC supports formats (h264, hevc)");
            });

        When(
            x => x.HardwareAcceleration == HardwareAccelerationKind.Vaapi,
            () =>
            {
                RuleFor(x => x.VideoFormat).Must(c => VaapiFormats.Contains(c))
                    .WithMessage("VAAPI supports formats (h264, hevc, mpeg2video)");
            });

        When(
            x => x.HardwareAcceleration == HardwareAccelerationKind.VideoToolbox,
            () =>
            {
                RuleFor(x => x.VideoFormat).Must(c => VideoToolboxFormats.Contains(c))
                    .WithMessage("VideoToolbox supports formats (h264, hevc)");
            });
    }
}
