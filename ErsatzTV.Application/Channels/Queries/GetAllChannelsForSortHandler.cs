using System.Globalization;
using ErsatzTV.Core.Domain;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Channels;

public class GetAllChannelsForSortHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<GetAllChannelsForSort, List<ChannelSortViewModel>>
{
    public async Task<List<ChannelSortViewModel>> Handle(
        GetAllChannelsForSort request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.Channels
            .AsNoTracking()
            .ToListAsync(cancellationToken)
            .Map(list => list.Map(ProjectToSortViewModel)
                .OrderBy(c => decimal.Parse(c.Number, CultureInfo.InvariantCulture)).ToList());
    }

    private static ChannelSortViewModel ProjectToSortViewModel(Channel channel)
        => new()
        {
            Id = channel.Id,
            Number = channel.Number,
            Name = channel.Name,
            OriginalNumber = channel.Number
        };
}
