using ErsatzTV.Core.Domain;
using ErsatzTV.FFmpeg.Capabilities;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.FFmpeg;

public class RefreshFFmpegCapabilitiesHandler(
    IDbContextFactory<TvContext> dbContextFactory,
    IHardwareCapabilitiesFactory hardwareCapabilitiesFactory)
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
        }
    }
}
