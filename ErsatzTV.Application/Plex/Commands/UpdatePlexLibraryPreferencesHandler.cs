using ErsatzTV.Core;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Search;

namespace ErsatzTV.Application.Plex;

public class
    UpdatePlexLibraryPreferencesHandler : IRequestHandler<UpdatePlexLibraryPreferences,
        Either<BaseError, Unit>>
{
    private readonly IMediaSourceRepository _mediaSourceRepository;
    private readonly ISearchIndex _searchIndex;

    public UpdatePlexLibraryPreferencesHandler(
        IMediaSourceRepository mediaSourceRepository,
        ISearchIndex searchIndex)
    {
        _mediaSourceRepository = mediaSourceRepository;
        _searchIndex = searchIndex;
    }

    public async Task<Either<BaseError, Unit>> Handle(
        UpdatePlexLibraryPreferences request,
        CancellationToken cancellationToken)
    {
        var toDisable = request.Preferences.Filter(p => p.ShouldSyncItems == false).Map(p => p.Id).ToList();
        List<int> ids = await _mediaSourceRepository.DisablePlexLibrarySync(toDisable);
        await _searchIndex.RemoveItems(ids);
        _searchIndex.Commit();

        IEnumerable<int> toEnable = request.Preferences.Filter(p => p.ShouldSyncItems).Map(p => p.Id);
        await _mediaSourceRepository.EnablePlexLibrarySync(toEnable);

        return Unit.Default;
    }
}
