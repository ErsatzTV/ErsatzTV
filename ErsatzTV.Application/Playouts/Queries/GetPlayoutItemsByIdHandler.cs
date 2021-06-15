﻿using System.Collections.Generic;
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
    public class GetPlayoutItemsByIdHandler : IRequestHandler<GetPlayoutItemsById, PagedPlayoutItemsViewModel>
    {
        private readonly IDbContextFactory<TvContext> _dbContextFactory;

        public GetPlayoutItemsByIdHandler(IDbContextFactory<TvContext> dbContextFactory) =>
            _dbContextFactory = dbContextFactory;

        public async Task<PagedPlayoutItemsViewModel> Handle(
            GetPlayoutItemsById request,
            CancellationToken cancellationToken)
        {
            await using TvContext dbContext = _dbContextFactory.CreateDbContext();

            int totalCount = await dbContext.PlayoutItems
                .CountAsync(i => i.PlayoutId == request.PlayoutId, cancellationToken);
            
            List<PlayoutItemViewModel> page = await dbContext.PlayoutItems
                .Include(i => i.MediaItem)
                .ThenInclude(mi => (mi as Movie).MovieMetadata)
                .Include(i => i.MediaItem)
                .ThenInclude(mi => (mi as Movie).MediaVersions)
                .Include(i => i.MediaItem)
                .ThenInclude(mi => (mi as MusicVideo).MusicVideoMetadata)
                .Include(i => i.MediaItem)
                .ThenInclude(mi => (mi as MusicVideo).MediaVersions)
                .Include(i => i.MediaItem)
                .ThenInclude(mi => (mi as MusicVideo).Artist)
                .ThenInclude(mm => mm.ArtistMetadata)
                .Include(i => i.MediaItem)
                .ThenInclude(mi => (mi as Episode).EpisodeMetadata)
                .Include(i => i.MediaItem)
                .ThenInclude(mi => (mi as Episode).MediaVersions)
                .Include(i => i.MediaItem)
                .ThenInclude(mi => (mi as Episode).Season)
                .ThenInclude(s => s.SeasonMetadata)
                .Include(i => i.MediaItem)
                .ThenInclude(mi => (mi as Episode).Season.Show)
                .ThenInclude(s => s.ShowMetadata)
                .Filter(i => i.PlayoutId == request.PlayoutId)
                .OrderBy(i => i.Start)
                .Skip(request.PageNum * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken)
                .Map(list => list.Map(ProjectToViewModel).ToList());

            return new PagedPlayoutItemsViewModel(totalCount, page);
        }
    }
}
