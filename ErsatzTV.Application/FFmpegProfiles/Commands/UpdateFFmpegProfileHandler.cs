using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using MediatR;
using static ErsatzTV.Application.FFmpegProfiles.Mapper;

namespace ErsatzTV.Application.FFmpegProfiles.Commands
{
    public class
        UpdateFFmpegProfileHandler : IRequestHandler<UpdateFFmpegProfile, Either<BaseError, FFmpegProfileViewModel>>
    {
        private readonly IFFmpegProfileRepository _ffmpegProfileRepository;
        private readonly IResolutionRepository _resolutionRepository;

        public UpdateFFmpegProfileHandler(
            IFFmpegProfileRepository ffmpegProfileRepository,
            IResolutionRepository resolutionRepository)
        {
            _ffmpegProfileRepository = ffmpegProfileRepository;
            _resolutionRepository = resolutionRepository;
        }

        public Task<Either<BaseError, FFmpegProfileViewModel>> Handle(
            UpdateFFmpegProfile request,
            CancellationToken cancellationToken) =>
            Validate(request)
                .MapT(c => ApplyUpdateRequest(c, request))
                .Bind(v => v.ToEitherAsync());

        private async Task<FFmpegProfileViewModel> ApplyUpdateRequest(FFmpegProfile p, UpdateFFmpegProfile update)
        {
            p.Name = update.Name;
            p.ThreadCount = update.ThreadCount;
            p.Transcode = update.Transcode;
            p.HardwareAcceleration = update.HardwareAcceleration;
            p.ResolutionId = update.ResolutionId;
            p.NormalizeVideo = update.NormalizeVideo;
            p.VideoCodec = update.VideoCodec;
            p.VideoBitrate = update.VideoBitrate;
            p.VideoBufferSize = update.VideoBufferSize;
            p.AudioCodec = update.AudioCodec;
            p.AudioBitrate = update.AudioBitrate;
            p.AudioBufferSize = update.AudioBufferSize;
            p.NormalizeLoudness = update.NormalizeLoudness;
            p.AudioChannels = update.AudioChannels;
            p.AudioSampleRate = update.AudioSampleRate;
            p.NormalizeAudio = update.NormalizeAudio;
            p.FrameRate = update.FrameRate;
            await _ffmpegProfileRepository.Update(p);
            return ProjectToViewModel(p);
        }

        private async Task<Validation<BaseError, FFmpegProfile>> Validate(UpdateFFmpegProfile request) =>
            (await FFmpegProfileMustExist(request), ValidateName(request), ValidateThreadCount(request),
                await ResolutionMustExist(request))
            .Apply((ffmpegProfileToUpdate, _, _, _) => ffmpegProfileToUpdate);

        private async Task<Validation<BaseError, FFmpegProfile>> FFmpegProfileMustExist(
            UpdateFFmpegProfile updateFFmpegProfile) =>
            (await _ffmpegProfileRepository.Get(updateFFmpegProfile.FFmpegProfileId))
            .ToValidation<BaseError>("FFmpegProfile does not exist.");

        private Validation<BaseError, string> ValidateName(UpdateFFmpegProfile updateFFmpegProfile) =>
            updateFFmpegProfile.NotEmpty(x => x.Name)
                .Bind(_ => updateFFmpegProfile.NotLongerThan(50)(x => x.Name));

        private Validation<BaseError, int> ValidateThreadCount(UpdateFFmpegProfile updateFFmpegProfile) =>
            updateFFmpegProfile.AtLeast(0)(p => p.ThreadCount);

        private async Task<Validation<BaseError, int>> ResolutionMustExist(UpdateFFmpegProfile updateFFmpegProfile) =>
            (await _resolutionRepository.Get(updateFFmpegProfile.ResolutionId))
            .ToValidation<BaseError>($"[Resolution] {updateFFmpegProfile.ResolutionId} does not exist.")
            .Map(c => c.Id);
    }
}
