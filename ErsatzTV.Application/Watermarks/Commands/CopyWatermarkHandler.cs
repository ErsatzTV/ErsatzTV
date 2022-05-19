using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using static ErsatzTV.Application.Watermarks.Mapper;

namespace ErsatzTV.Application.Watermarks;

public class CopyWatermarkHandler :
    IRequestHandler<CopyWatermark, Either<BaseError, WatermarkViewModel>>
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;

    public CopyWatermarkHandler(IDbContextFactory<TvContext> dbContextFactory) =>
        _dbContextFactory = dbContextFactory;

    public Task<Either<BaseError, WatermarkViewModel>> Handle(
        CopyWatermark request,
        CancellationToken cancellationToken) =>
        Validate(request)
            .MapT(PerformCopy)
            .Bind(v => v.ToEitherAsync());

    private async Task<WatermarkViewModel> PerformCopy(CopyWatermark request)
    {
        await using TvContext dbContext = _dbContextFactory.CreateDbContext();
        ChannelWatermark channelWatermark = await dbContext.ChannelWatermarks.FindAsync(request.WatermarkId);

        PropertyValues values = dbContext.Entry(channelWatermark).CurrentValues.Clone();
        values["Id"] = 0;

        var clone = new ChannelWatermark();
        await dbContext.AddAsync(clone);
        dbContext.Entry(clone).CurrentValues.SetValues(values);
        clone.Name = request.Name;

        await dbContext.SaveChangesAsync();

        return ProjectToViewModel(clone);
    }

    private static Task<Validation<BaseError, CopyWatermark>> Validate(CopyWatermark request) =>
        ValidateName(request).AsTask().MapT(_ => request);

    private static Validation<BaseError, string> ValidateName(CopyWatermark request) =>
        request.NotEmpty(x => x.Name)
            .Bind(_ => request.NotLongerThan(50)(x => x.Name));
}
