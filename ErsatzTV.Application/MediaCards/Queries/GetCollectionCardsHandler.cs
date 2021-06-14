using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using LanguageExt;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static ErsatzTV.Application.MediaCards.Mapper;

namespace ErsatzTV.Application.MediaCards.Queries
{
    public class GetCollectionCardsHandler :
        IRequestHandler<GetCollectionCards, Either<BaseError, CollectionCardResultsViewModel>>
    {
        private readonly IDbContextFactory<TvContext> _dbContextFactory;
        private readonly IMediaSourceRepository _mediaSourceRepository;

        public GetCollectionCardsHandler(
            IDbContextFactory<TvContext> dbContextFactory,
            IMediaSourceRepository mediaSourceRepository)
        {
            _dbContextFactory = dbContextFactory;
            _mediaSourceRepository = mediaSourceRepository;
        }

        public async Task<Either<BaseError, CollectionCardResultsViewModel>> Handle(
            GetCollectionCards request,
            CancellationToken cancellationToken)
        {
            await using TvContext dbContext = _dbContextFactory.CreateDbContext();

            Option<JellyfinMediaSource> maybeJellyfin = await _mediaSourceRepository.GetAllJellyfin()
                .Map(list => list.HeadOrNone());

            Option<EmbyMediaSource> maybeEmby = await _mediaSourceRepository.GetAllEmby()
                .Map(list => list.HeadOrNone());

            return await dbContext.Collections
                .AsNoTracking()
                .Include(c => c.CollectionItems)
                .Include(c => c.MediaItems)
                .ThenInclude(i => i.LibraryPath)
                .Include(c => c.MediaItems)
                .ThenInclude(i => (i as Movie).MovieMetadata)
                .ThenInclude(mm => mm.Artwork)
                .Include(c => c.MediaItems)
                .ThenInclude(i => (i as Artist).ArtistMetadata)
                .ThenInclude(mvm => mvm.Artwork)
                .Include(c => c.MediaItems)
                .ThenInclude(i => (i as MusicVideo).MusicVideoMetadata)
                .ThenInclude(mvm => mvm.Artwork)
                .Include(c => c.MediaItems)
                .ThenInclude(i => (i as MusicVideo).Artist)
                .ThenInclude(a => a.ArtistMetadata)
                .Include(c => c.MediaItems)
                .ThenInclude(i => (i as Show).ShowMetadata)
                .ThenInclude(sm => sm.Artwork)
                .Include(c => c.MediaItems)
                .ThenInclude(i => (i as Season).SeasonMetadata)
                .ThenInclude(sm => sm.Artwork)
                .Include(c => c.MediaItems)
                .ThenInclude(i => (i as Season).Show)
                .ThenInclude(s => s.ShowMetadata)
                .Include(c => c.MediaItems)
                .ThenInclude(i => (i as Episode).EpisodeMetadata)
                .ThenInclude(em => em.Artwork)
                .Include(c => c.MediaItems)
                .ThenInclude(i => (i as Episode).EpisodeMetadata)
                .ThenInclude(em => em.Directors)
                .Include(c => c.MediaItems)
                .ThenInclude(i => (i as Episode).EpisodeMetadata)
                .ThenInclude(em => em.Writers)
                .Include(c => c.MediaItems)
                .ThenInclude(i => (i as Episode).Season)
                .ThenInclude(s => s.Show)
                .ThenInclude(s => s.ShowMetadata)
                .Include(c => c.MediaItems)
                .ThenInclude(i => (i as Episode).Season)
                .ThenInclude(s => s.SeasonMetadata)
                .SelectOneAsync(c => c.Id, c => c.Id == request.Id)
                .Map(c => c.ToEither(BaseError.New("Unable to load collection")))
                .MapT(c => ProjectToViewModel(c, maybeJellyfin, maybeEmby));
        }
    }
}
