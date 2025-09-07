using ErsatzTV.Core.Domain;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Channels;

public class GetChannelResolutionAndBitrateHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<GetChannelResolutionAndBitrate, Option<ResolutionAndBitrateViewModel>>
{
    public async Task<Option<ResolutionAndBitrateViewModel>> Handle(
        GetChannelResolutionAndBitrate request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        Option<Channel> maybeChannel = await dbContext.Channels
            .AsNoTracking()
            .Include(c => c.FFmpegProfile)
            .ThenInclude(ff => ff.Resolution)
            .SelectOneAsync(c => c.Number, c => c.Number == request.ChannelNumber, cancellationToken);

        return maybeChannel.Map(c => Mapper.ProjectToViewModel(
            c.FFmpegProfile.Resolution,
            (int)((c.FFmpegProfile.VideoBitrate * 1000 + c.FFmpegProfile.AudioBitrate * 1000) * 1.2)));
    }
}
