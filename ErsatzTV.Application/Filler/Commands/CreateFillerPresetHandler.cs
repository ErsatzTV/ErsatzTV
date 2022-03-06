using Bugsnag;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain.Filler;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Filler;

public class CreateFillerPresetHandler : IRequestHandler<CreateFillerPreset, Either<BaseError, Unit>>
{
    private readonly IClient _client;
    private readonly IDbContextFactory<TvContext> _dbContextFactory;

    public CreateFillerPresetHandler(IClient client, IDbContextFactory<TvContext> dbContextFactory)
    {
        _client = client;
        _dbContextFactory = dbContextFactory;
    }

    public async Task<Either<BaseError, Unit>> Handle(CreateFillerPreset request, CancellationToken cancellationToken)
    {
        try
        {
            await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

            var fillerPreset = new FillerPreset
            {
                Name = request.Name,
                FillerKind = request.FillerKind,
                FillerMode = request.FillerMode,
                Duration = request.Duration,
                Count = request.Count,
                PadToNearestMinute = request.PadToNearestMinute,
                CollectionType = request.CollectionType,
                CollectionId = request.CollectionId,
                MediaItemId = request.MediaItemId,
                MultiCollectionId = request.MultiCollectionId,
                SmartCollectionId = request.SmartCollectionId
            };

            await dbContext.FillerPresets.AddAsync(fillerPreset, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);

            return Unit.Default;
        }
        catch (Exception ex)
        {
            _client.Notify(ex);
            return BaseError.New(ex.Message);
        }
    }
}