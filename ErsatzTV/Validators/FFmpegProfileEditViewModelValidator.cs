using ErsatzTV.ViewModels;
using FluentValidation;

namespace ErsatzTV.Validators
{
    public class FFmpegProfileEditViewModelValidator : AbstractValidator<FFmpegProfileEditViewModel>
    {
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
                x => x.QsvAcceleration,
                () =>
                {
                    RuleFor(x => x.VideoCodec).Must(c => c.EndsWith("_qsv"))
                        .WithMessage("QSV codec is required (h264_qsv, hevc_qsv, mpeg2_qsv)");
                });
        }
    }
}
