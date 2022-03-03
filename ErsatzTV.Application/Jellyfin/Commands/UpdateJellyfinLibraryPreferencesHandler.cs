using ErsatzTV.Core;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Search;

namespace ErsatzTV.Application.Jellyfin;

public class
    UpdateJellyfinLibraryPreferencesHandler : MediatR.IRequestHandler<UpdateJellyfinLibraryPreferences,
        Either<BaseError, Unit>>
{
    private readonly IMediaSourceRepository _mediaSourceRepository;
    private readonly ISearchIndex _searchIndex;

    public UpdateJellyfinLibraryPreferencesHandler(
        IMediaSourceRepository mediaSourceRepository,
        ISearchIndex searchIndex)
    {
        _mediaSourceRepository = mediaSourceRepository;
        _searchIndex = searchIndex;
    }

    public async Task<Either<BaseError, Unit>> Handle(
        UpdateJellyfinLibraryPreferences request,
        CancellationToken cancellationToken)
    {
        var toDisable = request.Preferences.Filter(p => p.ShouldSyncItems == false).Map(p => p.Id).ToList();
        List<int> ids = await _mediaSourceRepository.DisableJellyfinLibrarySync(toDisable);
        await _searchIndex.RemoveItems(ids);
        _searchIndex.Commit();

        IEnumerable<int> toEnable = request.Preferences.Filter(p => p.ShouldSyncItems).Map(p => p.Id);
        await _mediaSourceRepository.EnableJellyfinLibrarySync(toEnable);

        return Unit.Default;
    }
}