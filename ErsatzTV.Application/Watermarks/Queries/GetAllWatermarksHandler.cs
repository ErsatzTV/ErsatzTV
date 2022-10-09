using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using static ErsatzTV.Application.Watermarks.Mapper;

namespace ErsatzTV.Application.Watermarks;

public class GetAllWatermarksHandler : IRequestHandler<GetAllWatermarks, List<WatermarkViewModel>>
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;

    public GetAllWatermarksHandler(IDbContextFactory<TvContext> dbContextFactory) =>
        _dbContextFactory = dbContextFactory;

    public async Task<List<WatermarkViewModel>> Handle(
        GetAllWatermarks request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.ChannelWatermarks
            .ToListAsync(cancellationToken)
            .Map(list => list.Map(ProjectToViewModel).ToList());
    }
}
