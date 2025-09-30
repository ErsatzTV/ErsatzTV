using ErsatzTV.Core.Domain;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Channels;

public class GetChannelStreamingSpecsHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<GetChannelStreamingSpecs, Option<ChannelStreamingSpecsViewModel>>
{
    public async Task<Option<ChannelStreamingSpecsViewModel>> Handle(
        GetChannelStreamingSpecs request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        Option<Channel> maybeChannel = await dbContext.Channels
            .AsNoTracking()
            .Include(c => c.FFmpegProfile)
            .ThenInclude(ff => ff.Resolution)
            .SelectOneAsync(c => c.Number, c => c.Number == request.ChannelNumber, cancellationToken);

        return maybeChannel.Map(Mapper.ProjectToSpecsViewModel);
    }
}
