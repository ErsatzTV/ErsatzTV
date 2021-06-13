using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Infrastructure.Data;
using LanguageExt;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static ErsatzTV.Application.Playouts.Mapper;

namespace ErsatzTV.Application.Playouts.Queries
{
    public class GetPlayoutItemsByIdHandler : IRequestHandler<GetPlayoutItemsById, List<PlayoutItemViewModel>>
    {
        private readonly IDbContextFactory<TvContext> _dbContextFactory;

        public GetPlayoutItemsByIdHandler(IDbContextFactory<TvContext> dbContextFactory) =>
            _dbContextFactory = dbContextFactory;

        public async Task<List<PlayoutItemViewModel>> Handle(
            GetPlayoutItemsById request,
            CancellationToken cancellationToken)
        {
            await using TvContext dbContext = _dbContextFactory.CreateDbContext();
            return await dbContext.PlayoutItems
                .Include(i => i.MediaItem)
                .ThenInclude(mi => (mi as Movie).MovieMetadata)
                .ThenInclude(mm => mm.Artwork)
                .Include(i => i.MediaItem)
                .ThenInclude(mi => (mi as Movie).MediaVersions)
                .Include(i => i.MediaItem)
                .ThenInclude(mi => (mi as MusicVideo).MusicVideoMetadata)
                .ThenInclude(mm => mm.Artwork)
                .Include(i => i.MediaItem)
                .ThenInclude(mi => (mi as MusicVideo).MediaVersions)
                .Include(i => i.MediaItem)
                .ThenInclude(mi => (mi as MusicVideo).Artist)
                .ThenInclude(mm => mm.ArtistMetadata)
                .Include(i => i.MediaItem)
                .ThenInclude(mi => (mi as Episode).EpisodeMetadata)
                .ThenInclude(em => em.Artwork)
                .Include(i => i.MediaItem)
                .ThenInclude(mi => (mi as Episode).MediaVersions)
                .Include(i => i.MediaItem)
                .ThenInclude(mi => (mi as Episode).Season)
                .ThenInclude(s => s.SeasonMetadata)
                .Include(i => i.MediaItem)
                .ThenInclude(mi => (mi as Episode).Season.Show)
                .ThenInclude(s => s.ShowMetadata)
                .Filter(i => i.PlayoutId == request.PlayoutId)
                .ToListAsync(cancellationToken)
                .Map(list => list.Map(ProjectToViewModel).ToList());
        }
    }
}
