using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Search;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.FFmpegProfiles;

public class CreateFFmpegProfileHandler :
    IRequestHandler<CreateFFmpegProfile, Either<BaseError, CreateFFmpegProfileResult>>
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;
    private readonly ISearchTargets _searchTargets;

    public CreateFFmpegProfileHandler(IDbContextFactory<TvContext> dbContextFactory, ISearchTargets searchTargets)
    {
        _dbContextFactory = dbContextFactory;
        _searchTargets = searchTargets;
    }

    public async Task<Either<BaseError, CreateFFmpegProfileResult>> Handle(
        CreateFFmpegProfile request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        Validation<BaseError, FFmpegProfile> validation = await Validate(dbContext, request);
        return await validation.Apply(profile => PersistFFmpegProfile(dbContext, profile));
    }

    private async Task<CreateFFmpegProfileResult> PersistFFmpegProfile(
        TvContext dbContext,
        FFmpegProfile ffmpegProfile)
    {
        await dbContext.FFmpegProfiles.AddAsync(ffmpegProfile);
        await dbContext.SaveChangesAsync();
        _searchTargets.SearchTargetsChanged();
        return new CreateFFmpegProfileResult(ffmpegProfile.Id);
    }

    private static async Task<Validation<BaseError, FFmpegProfile>> Validate(
        TvContext dbContext,
        CreateFFmpegProfile request) =>
        (ValidateName(request), ValidateThreadCount(request), await ResolutionMustExist(dbContext, request))
        .Apply(
            (name, threadCount, resolutionId) => new FFmpegProfile
            {
                Name = name,
                ThreadCount = threadCount,
                HardwareAcceleration = request.HardwareAcceleration,
                VaapiDriver = request.VaapiDriver,
                VaapiDevice = request.VaapiDevice,
                QsvExtraHardwareFrames = request.QsvExtraHardwareFrames,
                ResolutionId = resolutionId,
                ScalingBehavior = request.ScalingBehavior,
                VideoFormat = request.VideoFormat,
                BitDepth = request.BitDepth,
                VideoBitrate = request.VideoBitrate,
                VideoBufferSize = request.VideoBufferSize,
                AudioFormat = request.AudioFormat,
                AudioBitrate = request.AudioBitrate,
                AudioBufferSize = request.AudioBufferSize,
                NormalizeLoudnessMode = request.NormalizeLoudnessMode,
                AudioChannels = request.AudioChannels,
                AudioSampleRate = request.AudioSampleRate,
                NormalizeFramerate = request.NormalizeFramerate,
                DeinterlaceVideo = request.DeinterlaceVideo
            });

    private static Validation<BaseError, string> ValidateName(CreateFFmpegProfile createFFmpegProfile) =>
        createFFmpegProfile.NotEmpty(x => x.Name)
            .Bind(_ => createFFmpegProfile.NotLongerThan(50)(x => x.Name));

    private static Validation<BaseError, int> ValidateThreadCount(CreateFFmpegProfile createFFmpegProfile) =>
        createFFmpegProfile.AtLeast(0)(p => p.ThreadCount);

    private static Task<Validation<BaseError, int>> ResolutionMustExist(
        TvContext dbContext,
        CreateFFmpegProfile createFFmpegProfile) =>
        dbContext.Resolutions
            .SelectOneAsync(r => r.Id, r => r.Id == createFFmpegProfile.ResolutionId)
            .MapT(r => r.Id)
            .Map(o => o.ToValidation<BaseError>($"[Resolution] {createFFmpegProfile.ResolutionId} does not exist"));
}
