using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Extensions;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Application.Channels;

public class GetChannelFramerateHandler : IRequestHandler<GetChannelFramerate, Option<int>>
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;
    private readonly ILogger<GetChannelFramerateHandler> _logger;

    public GetChannelFramerateHandler(
        IDbContextFactory<TvContext> dbContextFactory,
        ILogger<GetChannelFramerateHandler> logger)
    {
        _dbContextFactory = dbContextFactory;
        _logger = logger;
    }

    public async Task<Option<int>> Handle(GetChannelFramerate request, CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        FFmpegProfile ffmpegProfile = await dbContext.Channels
            .Filter(c => c.Number == request.ChannelNumber)
            .Include(c => c.FFmpegProfile)
            .Map(c => c.FFmpegProfile)
            .SingleAsync(cancellationToken);

        if (!ffmpegProfile.NormalizeFramerate)
        {
            return Option<int>.None;
        }

        // TODO: expand to check everything in collection rather than what's scheduled?
        _logger.LogDebug("Checking frame rates for channel {ChannelNumber}", request.ChannelNumber);

        List<Playout> playouts = await dbContext.Playouts
            .Include(p => p.Items)
            .ThenInclude(pi => pi.MediaItem)
            .ThenInclude(mi => (mi as Movie).MediaVersions)
            .Include(p => p.Items)
            .ThenInclude(pi => pi.MediaItem)
            .ThenInclude(mi => (mi as Episode).MediaVersions)
            .Include(p => p.Items)
            .ThenInclude(pi => pi.MediaItem)
            .ThenInclude(mi => (mi as Song).MediaVersions)
            .Include(p => p.Items)
            .ThenInclude(pi => pi.MediaItem)
            .ThenInclude(mi => (mi as MusicVideo).MediaVersions)
            .Include(p => p.Items)
            .ThenInclude(pi => pi.MediaItem)
            .ThenInclude(mi => (mi as OtherVideo).MediaVersions)
            .Filter(p => p.Channel.Number == request.ChannelNumber)
            .ToListAsync(cancellationToken);

        var frameRates = playouts.Map(p => p.Items.Map(i => i.MediaItem.GetHeadVersion()))
            .Flatten()
            .Map(mv => mv.RFrameRate)
            .ToList();

        var distinct = frameRates.Distinct().ToList();
        if (distinct.Count > 1)
        {
            // TODO: something more intelligent than minimum framerate?
            int result = frameRates.Map(ParseFrameRate).Min();
            if (result < 24)
            {
                _logger.LogInformation(
                    "Normalizing frame rate for channel {ChannelNumber} from {Distinct} to {FrameRate} instead of min value {MinFrameRate}",
                    request.ChannelNumber,
                    distinct,
                    24,
                    result);

                return 24;
            }

            _logger.LogInformation(
                "Normalizing frame rate for channel {ChannelNumber} from {Distinct} to {FrameRate}",
                request.ChannelNumber,
                distinct,
                result);
            return result;
        }

        _logger.LogInformation(
            "All content on channel {ChannelNumber} has the same frame rate of {FrameRate}; will not normalize",
            request.ChannelNumber,
            distinct[0]);
        return None;
    }

    private int ParseFrameRate(string frameRate)
    {
        if (!int.TryParse(frameRate, out int fr))
        {
            string[] split = (frameRate ?? string.Empty).Split("/");
            if (int.TryParse(split[0], out int left) && int.TryParse(split[1], out int right))
            {
                fr = (int)Math.Round(left / (double)right);
            }
            else
            {
                fr = 24;
            }
        }

        return fr;
    }
}
