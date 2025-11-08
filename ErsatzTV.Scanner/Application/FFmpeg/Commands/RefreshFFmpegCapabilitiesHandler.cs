using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.FFmpeg.Capabilities;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Scanner.Application.FFmpeg;

public class RefreshFFmpegCapabilitiesHandler(
    IDbContextFactory<TvContext> dbContextFactory,
    IHardwareCapabilitiesFactory hardwareCapabilitiesFactory,
    ILocalStatisticsProvider localStatisticsProvider)
    : IRequestHandler<RefreshFFmpegCapabilities>
{
    public async Task Handle(RefreshFFmpegCapabilities request, CancellationToken cancellationToken)
    {
        hardwareCapabilitiesFactory.ClearCache();

        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        Option<string> maybeFFmpegPath = await dbContext.ConfigElements
            .GetValue<string>(ConfigElementKey.FFmpegPath, cancellationToken)
            .FilterT(File.Exists);

        foreach (string ffmpegPath in maybeFFmpegPath)
        {
            _ = await hardwareCapabilitiesFactory.GetFFmpegCapabilities(ffmpegPath);

            Option<string> maybeFFprobePath = await dbContext.ConfigElements
                .GetValue<string>(ConfigElementKey.FFprobePath, cancellationToken)
                .FilterT(File.Exists);

            foreach (string ffprobePath in maybeFFprobePath)
            {
                Either<BaseError, MediaVersion> result = await localStatisticsProvider.GetStatistics(
                    ffprobePath,
                    Path.Combine(FileSystemLayout.ResourcesCacheFolder, "test.avs"));

                hardwareCapabilitiesFactory.SetAviSynthInstalled(result.IsRight);
            }
        }
    }
}
