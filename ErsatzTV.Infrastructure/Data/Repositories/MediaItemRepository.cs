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
            _dbContext.MediaItems.SingleOrDefaultAsync(i => i.Id == id).Map(Optional);

        public Task<List<MediaItem>> GetAll() => _dbContext.MediaItems.ToListAsync();

        public Task<List<MediaItem>> Search(string searchString)
        {
            IQueryable<MediaItem> data = from c in _dbContext.MediaItems.Include(c => c.Source) select c;

            if (!string.IsNullOrEmpty(searchString))
            {
                data = data.Where(c => EF.Functions.Like(c.Metadata.Title, $"%{searchString}%"));
            }

            return data.ToListAsync();
        }


        public Task<List<MediaItemSummary>> GetPageByType(MediaType mediaType, int pageNumber, int pageSize) =>
            mediaType switch
            {
                MediaType.Movie => _dbContext.MediaItemSummaries.FromSqlRaw(
                        @"SELECT Metadata_Title AS Title, Metadata_SortTitle AS SortTitle, substr(Metadata_Aired, 1, 4) AS Subtitle
FROM MediaItems WHERE Metadata_MediaType=2
ORDER BY Metadata_SortTitle
LIMIT {0} OFFSET {1}",
                        pageSize,
                        (pageNumber - 1) * pageSize)
                    .AsNoTracking()
                    .ToListAsync(),
                MediaType.TvShow => _dbContext.MediaItemSummaries.FromSqlRaw(
                        @"SELECT Metadata_Title AS Title, Metadata_SortTitle AS SortTitle, count(*) || ' Episodes' AS Subtitle
FROM MediaItems WHERE Metadata_MediaType=1
GROUP BY Metadata_Title, Metadata_SortTitle
ORDER BY Metadata_SortTitle
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
                MediaType.Movie => _dbContext.MediaItems
                    .Filter(i => i.Metadata.MediaType == mediaType)
                    .CountAsync(),
                MediaType.TvShow => _dbContext.MediaItems
                    .Filter(i => i.Metadata.MediaType == mediaType)
                    .GroupBy(i => new { i.Metadata.Title, i.Metadata.SortTitle })
                    .CountAsync(),
                _ => Task.FromResult(0)
            };

        public Task<List<MediaItem>> GetAllByMediaSourceId(int mediaSourceId) =>
            _dbContext.MediaItems
                .Filter(i => i.MediaSourceId == mediaSourceId)
                .ToListAsync();

        public async Task Update(MediaItem mediaItem)
        {
            _dbContext.MediaItems.Update(mediaItem);
            await _dbContext.SaveChangesAsync();
        }

        public async Task Delete(int mediaItemId)
        {
            MediaItem mediaItem = await _dbContext.MediaItems.FindAsync(mediaItemId);
            _dbContext.MediaItems.Remove(mediaItem);
            await _dbContext.SaveChangesAsync();
        }
    }
}
