using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.FFmpeg;
using ErsatzTV.FFmpeg.Capabilities;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.FFmpegProfiles;

public class
    GetSupportedHardwareAccelerationKindsHandler : IRequestHandler<GetSupportedHardwareAccelerationKinds,
        List<HardwareAccelerationKind>>
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;
    private readonly IHardwareCapabilitiesFactory _hardwareCapabilitiesFactory;

    public GetSupportedHardwareAccelerationKindsHandler(
        IDbContextFactory<TvContext> dbContextFactory,
        IHardwareCapabilitiesFactory hardwareCapabilitiesFactory)
    {
        _dbContextFactory = dbContextFactory;
        _hardwareCapabilitiesFactory = hardwareCapabilitiesFactory;
    }

    public async Task<List<HardwareAccelerationKind>> Handle(
        GetSupportedHardwareAccelerationKinds request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        Validation<BaseError, string> validation = await Validate(dbContext);

        return await validation.Match(
            GetHardwareAccelerationKinds,
            _ => Task.FromResult(new List<HardwareAccelerationKind> { HardwareAccelerationKind.None }));
    }

    private async Task<List<HardwareAccelerationKind>> GetHardwareAccelerationKinds(string ffmpegPath)
    {
        var result = new List<HardwareAccelerationKind> { HardwareAccelerationKind.None };

        IFFmpegCapabilities ffmpegCapabilities = await _hardwareCapabilitiesFactory.GetFFmpegCapabilities(ffmpegPath);

        if (ffmpegCapabilities.HasHardwareAcceleration(HardwareAccelerationMode.Nvenc))
        {
            result.Add(HardwareAccelerationKind.Nvenc);
        }

        if (ffmpegCapabilities.HasHardwareAcceleration(HardwareAccelerationMode.Qsv))
        {
            result.Add(HardwareAccelerationKind.Qsv);
        }

        if (ffmpegCapabilities.HasHardwareAcceleration(HardwareAccelerationMode.Vaapi))
        {
            result.Add(HardwareAccelerationKind.Vaapi);
        }

        if (ffmpegCapabilities.HasHardwareAcceleration(HardwareAccelerationMode.VideoToolbox))
        {
            result.Add(HardwareAccelerationKind.VideoToolbox);
        }

        if (ffmpegCapabilities.HasHardwareAcceleration(HardwareAccelerationMode.Amf))
        {
            result.Add(HardwareAccelerationKind.Amf);
        }

        return result;
    }

    private static async Task<Validation<BaseError, string>> Validate(TvContext dbContext) =>
        await FFmpegPathMustExist(dbContext);

    private static Task<Validation<BaseError, string>> FFmpegPathMustExist(TvContext dbContext) =>
        dbContext.ConfigElements.GetValue<string>(ConfigElementKey.FFmpegPath)
            .FilterT(File.Exists)
            .Map(maybePath => maybePath.ToValidation<BaseError>("FFmpeg path does not exist on filesystem"));
}
