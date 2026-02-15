using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Channels;

public class GetSlugSecondsByChannelNumberHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<GetSlugSecondsByChannelNumber, Option<double>>
{
    public async Task<Option<double>> Handle(GetSlugSecondsByChannelNumber request, CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.Channels
            .AsNoTracking()
            .SingleOrDefaultAsync(c => c.Number == request.ChannelNumber, cancellationToken)
            .Map(c => Optional(c.SlugSeconds));
    }
}
