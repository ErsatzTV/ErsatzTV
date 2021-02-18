using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ErsatzTV.Core.AggregateModels;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using Microsoft.EntityFrameworkCore;
using static LanguageExt.Prelude;

namespace ErsatzTV.Infrastructure.Data.Repositories
{
    public class MediaItemRepository : IMediaItemRepository
    {
        private readonly TvContext _dbContext;

        public MediaItemRepository(TvContext dbContext) => _dbContext = dbContext;

        public async Task<int> Add(MediaItem mediaItem)
        {
            await _dbContext.MediaItems.AddAsync(mediaItem);
            await _dbContext.SaveChangesAsync();
            return mediaItem.Id;
        }

        public Task<Option<MediaItem>> Get(int id) =>
            _dbContext.MediaItems
                .Include(i => i.Source)
                .SingleOrDefaultAsync(i => i.Id == id)
                .Map(Optional);

        public Task<List<MediaItem>> GetAll() => _dbContext.MediaItems.ToListAsync();

        public Task<List<MediaItem>> Search(string searchString)
        {
            IQueryable<TelevisionEpisodeMediaItem> episodeData =
                from c in _dbContext.TelevisionEpisodeMediaItems.Include(c => c.Source) select c;

            if (!string.IsNullOrEmpty(searchString))
            {
                episodeData = episodeData.Where(c => EF.Functions.Like(c.Metadata.Title, $"%{searchString}%"));
            }

            IQueryable<MovieMediaItem> movieData =
                from c in _dbContext.MovieMediaItems.Include(c => c.Source) select c;

            if (!string.IsNullOrEmpty(searchString))
            {
                movieData = movieData.Where(c => EF.Functions.Like(c.Metadata.Title, $"%{searchString}%"));
            }

            return episodeData.OfType<MediaItem>().Concat(movieData.OfType<MediaItem>()).ToListAsync();
        }


        public Task<List<MediaItemSummary>> GetPageByType(MediaType mediaType, int pageNumber, int pageSize) =>
            mediaType switch
            {
                MediaType.Movie => _dbContext.MediaItemSummaries.FromSqlRaw(
                        @"SELECT
    m.Id AS MediaItemId,
    mm.Title AS Title,
    mm.SortTitle AS SortTitle,
    mm.Year AS Subtitle,
    mi.Poster AS Poster
FROM Movies m
INNER JOIN MediaItems mi on m.Id = mi.Id
INNER JOIN MovieMetadata mm on mm.MovieId = m.Id
ORDER BY SortTitle
LIMIT {0} OFFSET {1}",
                        pageSize,
                        (pageNumber - 1) * pageSize)
                    .AsNoTracking()
                    .ToListAsync(),
                MediaType.TvShow => _dbContext.MediaItemSummaries.FromSqlRaw(
                        @"SELECT
    min(ts.Id) AS MediaItemId,
    tsm.Title AS Title,
    tsm.SortTitle AS SortTitle,
    tsm.Year AS Subtitle,
    max(ts.Poster) AS Poster
FROM TelevisionShows ts
INNER JOIN TelevisionShowMetadata tsm on tsm.TelevisionShowId = ts.Id
GROUP BY tsm.Title, tsm.SortTitle, tsm.Year
ORDER BY tsm.SortTitle
LIMIT {0} OFFSET {1}",
                        pageSize,
                        (pageNumber - 1) * pageSize)
                    .AsNoTracking()
                    .ToListAsync(),
                _ => Task.FromResult(new List<MediaItemSummary>())
            };

        public Task<int> GetCountByType(MediaType mediaType) =>
            mediaType switch
            {
                MediaType.Movie => _dbContext.MovieMediaItems
                    .CountAsync(),
                MediaType.TvShow => _dbContext.TelevisionShows
                    .GroupBy(i => new { i.Metadata.Title, i.Metadata.SortTitle })
                    .CountAsync(),
                _ => Task.FromResult(0)
            };

        public Task<List<MediaItem>> GetAllByMediaSourceId(int mediaSourceId) =>
            _dbContext.MediaItems
                .Filter(i => i.MediaSourceId == mediaSourceId)
                .ToListAsync();

        public async Task<bool> Update(MediaItem mediaItem)
        {
            _dbContext.MediaItems.Update(mediaItem);
            return await _dbContext.SaveChangesAsync() > 0;
        }

        public Task<Unit> Delete(int mediaItemId) =>
            _dbContext.MediaItems.FindAsync(mediaItemId).AsTask().Bind(
                mediaItem =>
                {
                    _dbContext.MediaItems.Remove(mediaItem);
                    return _dbContext.SaveChangesAsync();
                }).ToUnit();
    }
}
