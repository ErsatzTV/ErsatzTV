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
        CreateFFmpegProfileHandler : IRequestHandler<CreateFFmpegProfile, Either<BaseError, FFmpegProfileViewModel>>
    {
        private readonly IFFmpegProfileRepository _ffmpegProfileRepository;
        private readonly IResolutionRepository _resolutionRepository;

        public CreateFFmpegProfileHandler(
            IFFmpegProfileRepository ffmpegProfileRepository,
            IResolutionRepository resolutionRepository)
        {
            _ffmpegProfileRepository = ffmpegProfileRepository;
            _resolutionRepository = resolutionRepository;
        }

        public Task<Either<BaseError, FFmpegProfileViewModel>> Handle(
            CreateFFmpegProfile request,
            CancellationToken cancellationToken) =>
            Validate(request)
                .MapT(PersistFFmpegProfile)
                .Bind(v => v.ToEitherAsync());

        private Task<FFmpegProfileViewModel> PersistFFmpegProfile(FFmpegProfile ffmpegProfile) =>
            _ffmpegProfileRepository.Add(ffmpegProfile).Map(ProjectToViewModel);

        private async Task<Validation<BaseError, FFmpegProfile>> Validate(CreateFFmpegProfile request) =>
            (ValidateName(request), ValidateThreadCount(request), await ResolutionMustExist(request))
            .Apply(
                (name, threadCount, resolutionId) => new FFmpegProfile
                {
                    Name = name,
                    ThreadCount = threadCount,
                    Transcode = request.Transcode,
                    HardwareAcceleration = request.HardwareAcceleration,
                    ResolutionId = resolutionId,
                    NormalizeResolution = request.NormalizeResolution,
                    VideoCodec = request.VideoCodec,
                    NormalizeVideoCodec = request.NormalizeVideoCodec,
                    VideoBitrate = request.VideoBitrate,
                    VideoBufferSize = request.VideoBufferSize,
                    AudioCodec = request.AudioCodec,
                    NormalizeAudioCodec = request.NormalizeAudioCodec,
                    AudioBitrate = request.AudioBitrate,
                    AudioBufferSize = request.AudioBufferSize,
                    AudioVolume = request.AudioVolume,
                    AudioChannels = request.AudioChannels,
                    AudioSampleRate = request.AudioSampleRate,
                    NormalizeAudio = request.NormalizeAudio
                });

        private Validation<BaseError, string> ValidateName(CreateFFmpegProfile createFFmpegProfile) =>
            createFFmpegProfile.NotEmpty(x => x.Name)
                .Bind(_ => createFFmpegProfile.NotLongerThan(50)(x => x.Name));

        private Validation<BaseError, int> ValidateThreadCount(CreateFFmpegProfile createFFmpegProfile) =>
            createFFmpegProfile.AtLeast(1)(p => p.ThreadCount);

        private async Task<Validation<BaseError, int>> ResolutionMustExist(CreateFFmpegProfile createFFmpegProfile) =>
            (await _resolutionRepository.Get(createFFmpegProfile.ResolutionId))
            .ToValidation<BaseError>($"[Resolution] {createFFmpegProfile.ResolutionId} does not exist.")
            .Map(c => c.Id);
    }
}
