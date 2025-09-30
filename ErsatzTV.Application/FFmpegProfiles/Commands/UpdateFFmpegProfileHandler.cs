using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.FFmpeg;
using ErsatzTV.Core.Interfaces.Search;
using ErsatzTV.FFmpeg;
using ErsatzTV.FFmpeg.Format;
using ErsatzTV.FFmpeg.Preset;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.FFmpegProfiles;

public class
    UpdateFFmpegProfileHandler : IRequestHandler<UpdateFFmpegProfile, Either<BaseError, UpdateFFmpegProfileResult>>
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;
    private readonly ISearchTargets _searchTargets;

    public UpdateFFmpegProfileHandler(IDbContextFactory<TvContext> dbContextFactory, ISearchTargets searchTargets)
    {
        _dbContextFactory = dbContextFactory;
        _searchTargets = searchTargets;
    }

    public async Task<Either<BaseError, UpdateFFmpegProfileResult>> Handle(
        UpdateFFmpegProfile request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        Validation<BaseError, FFmpegProfile> validation = await Validate(dbContext, request, cancellationToken);
        return await validation.Apply(p => ApplyUpdateRequest(dbContext, p, request, cancellationToken));
    }

    private async Task<UpdateFFmpegProfileResult> ApplyUpdateRequest(
        TvContext dbContext,
        FFmpegProfile p,
        UpdateFFmpegProfile update,
        CancellationToken cancellationToken)
    {
        p.Name = update.Name;
        p.ThreadCount = update.ThreadCount;
        p.HardwareAcceleration = update.HardwareAcceleration;
        p.VaapiDisplay = update.VaapiDisplay;
        p.VaapiDriver = update.VaapiDriver;
        p.VaapiDevice = update.VaapiDevice;
        p.QsvExtraHardwareFrames = update.QsvExtraHardwareFrames;
        p.ResolutionId = update.ResolutionId;
        p.ScalingBehavior = update.ScalingBehavior;
        p.VideoFormat = update.VideoFormat;
        p.VideoProfile = update.VideoProfile;
        p.VideoPreset = update.VideoPreset;
        p.AllowBFrames = update.AllowBFrames;

        // mpeg2video only supports 8-bit content
        p.BitDepth = update.VideoFormat is FFmpegProfileVideoFormat.Mpeg2Video
            ? FFmpegProfileBitDepth.EightBit
            : update.BitDepth;

        if (p.HardwareAcceleration is not (HardwareAccelerationKind.Nvenc or HardwareAccelerationKind.Vaapi
                or HardwareAccelerationKind.Qsv) &&
            p.VideoFormat is FFmpegProfileVideoFormat.Av1)
        {
            p.VideoFormat = FFmpegProfileVideoFormat.Hevc;
        }

        p.VideoBitrate = update.VideoBitrate;
        p.VideoBufferSize = update.VideoBufferSize;
        p.TonemapAlgorithm = update.TonemapAlgorithm;
        p.AudioFormat = update.AudioFormat;
        p.AudioBitrate = update.AudioBitrate;
        p.AudioBufferSize = update.AudioBufferSize;
        p.NormalizeLoudnessMode = update.NormalizeLoudnessMode;
        p.AudioChannels = update.AudioChannels;
        p.AudioSampleRate = update.AudioSampleRate;
        p.NormalizeFramerate = update.NormalizeFramerate;
        p.DeinterlaceVideo = update.DeinterlaceVideo;

        // don't save invalid preset
        ICollection<string> presets = FFmpegLibraryHelper.PresetsForFFmpegProfile(
            p.HardwareAcceleration,
            p.VideoFormat,
            p.BitDepth);

        if (!presets.Contains(p.VideoPreset))
        {
            p.VideoPreset = VideoPreset.Unset;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        _searchTargets.SearchTargetsChanged();

        return new UpdateFFmpegProfileResult(p.Id);
    }

    private static async Task<Validation<BaseError, FFmpegProfile>> Validate(
        TvContext dbContext,
        UpdateFFmpegProfile request,
        CancellationToken cancellationToken) =>
        (await FFmpegProfileMustExist(dbContext, request, cancellationToken), ValidateName(request),
            ValidateThreadCount(request),
            await ResolutionMustExist(dbContext, request, cancellationToken))
        .Apply((ffmpegProfileToUpdate, _, _, _) => ffmpegProfileToUpdate);

    private static Task<Validation<BaseError, FFmpegProfile>> FFmpegProfileMustExist(
        TvContext dbContext,
        UpdateFFmpegProfile updateFFmpegProfile,
        CancellationToken cancellationToken) =>
        dbContext.FFmpegProfiles
            .SelectOneAsync(p => p.Id, p => p.Id == updateFFmpegProfile.FFmpegProfileId, cancellationToken)
            .Map(o => o.ToValidation<BaseError>("FFmpegProfile does not exist."));

    private static Validation<BaseError, string> ValidateName(UpdateFFmpegProfile updateFFmpegProfile) =>
        updateFFmpegProfile.NotEmpty(x => x.Name)
            .Bind(_ => updateFFmpegProfile.NotLongerThan(50)(x => x.Name));

    private static Validation<BaseError, int> ValidateThreadCount(UpdateFFmpegProfile updateFFmpegProfile) =>
        updateFFmpegProfile.AtLeast(0)(p => p.ThreadCount);

    private static Task<Validation<BaseError, int>> ResolutionMustExist(
        TvContext dbContext,
        UpdateFFmpegProfile updateFFmpegProfile,
        CancellationToken cancellationToken) =>
        dbContext.Resolutions
            .SelectOneAsync(r => r.Id, r => r.Id == updateFFmpegProfile.ResolutionId, cancellationToken)
            .MapT(r => r.Id)
            .Map(o => o.ToValidation<BaseError>($"[Resolution] {updateFFmpegProfile.ResolutionId} does not exist"));
}
