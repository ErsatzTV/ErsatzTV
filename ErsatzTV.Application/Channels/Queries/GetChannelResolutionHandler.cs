using ErsatzTV.Core.Domain;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Channels;

public class GetChannelResolutionHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<GetChannelResolution, Option<ResolutionViewModel>>
{
    public async Task<Option<ResolutionViewModel>> Handle(
        GetChannelResolution request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        Option<Channel> maybeChannel = await dbContext.Channels
            .AsNoTracking()
            .Include(c => c.FFmpegProfile)
            .ThenInclude(ff => ff.Resolution)
            .SelectOneAsync(c => c.Number, c => c.Number == request.ChannelNumber);

        return maybeChannel.Map(c => Mapper.ProjectToViewModel(c.FFmpegProfile.Resolution));
    }
}
