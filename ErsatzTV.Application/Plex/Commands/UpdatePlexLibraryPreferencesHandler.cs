using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;

namespace ErsatzTV.Application.Plex.Commands
{
    public class
        UpdatePlexLibraryPreferencesHandler : MediatR.IRequestHandler<UpdatePlexLibraryPreferences,
            Either<BaseError, Unit>>
    {
        private readonly IMediaSourceRepository _mediaSourceRepository;

        public UpdatePlexLibraryPreferencesHandler(IMediaSourceRepository mediaSourceRepository) =>
            _mediaSourceRepository = mediaSourceRepository;

        public async Task<Either<BaseError, Unit>> Handle(
            UpdatePlexLibraryPreferences request,
            CancellationToken cancellationToken)
        {
            IEnumerable<int> toDisable = request.Preferences.Filter(p => p.ShouldSyncItems == false).Map(p => p.Id);
            await _mediaSourceRepository.DisablePlexLibrarySync(toDisable);

            IEnumerable<int> toEnable = request.Preferences.Filter(p => p.ShouldSyncItems).Map(p => p.Id);
            await _mediaSourceRepository.EnablePlexLibrarySync(toEnable);

            return Unit.Default;
        }
    }
}
