using ErsatzTV.Core.Domain;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.MediaCollections;

public class GetPlaylistItemsHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<GetPlaylistItems, List<PlaylistItemViewModel>>
{
    public async Task<List<PlaylistItemViewModel>> Handle(GetPlaylistItems request, CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        List<PlaylistItem> allItems = await dbContext.PlaylistItems
            .AsNoTracking()
            .Filter(i => i.PlaylistId == request.PlaylistId)
            .Include(i => i.Collection)
            .Include(i => i.MultiCollection)
            .Include(i => i.SmartCollection)
            .Include(i => i.MediaItem)
            .ThenInclude(i => (i as Season).SeasonMetadata)
            .ThenInclude(sm => sm.Artwork)
            .Include(i => i.MediaItem)
            .ThenInclude(i => (i as Season).Show)
            .ThenInclude(s => s.ShowMetadata)
            .ThenInclude(sm => sm.Artwork)
            .Include(i => i.MediaItem)
            .ThenInclude(i => (i as Show).ShowMetadata)
            .ThenInclude(sm => sm.Artwork)
            .Include(i => i.MediaItem)
            .ThenInclude(i => (i as Artist).ArtistMetadata)
            .ThenInclude(am => am.Artwork)
            .Include(i => i.MediaItem)
            .ThenInclude(i => (i as Movie).MovieMetadata)
            .ThenInclude(mm => mm.Artwork)
            .Include(i => i.MediaItem)
            .ThenInclude(i => (i as Episode).EpisodeMetadata)
            .ThenInclude(mm => mm.Artwork)
            .Include(i => i.MediaItem)
            .ThenInclude(i => (i as Episode).Season)
            .ThenInclude(s => s.Show)
            .ThenInclude(s => s.ShowMetadata)
            .Include(i => i.MediaItem)
            .ThenInclude(i => (i as MusicVideo).MusicVideoMetadata)
            .ThenInclude(mm => mm.Artwork)
            .Include(i => i.MediaItem)
            .ThenInclude(i => (i as MusicVideo).Artist)
            .ThenInclude(a => a.ArtistMetadata)
            .Include(i => i.MediaItem)
            .ThenInclude(i => (i as OtherVideo).OtherVideoMetadata)
            .ThenInclude(mm => mm.Artwork)
            .Include(i => i.MediaItem)
            .ThenInclude(i => (i as Song).SongMetadata)
            .ThenInclude(mm => mm.Artwork)
            .Include(i => i.MediaItem)
            .ThenInclude(i => (i as Image).ImageMetadata)
            .ThenInclude(mm => mm.Artwork)
            .ToListAsync(cancellationToken);

        if (allItems.All(bi => !bi.IncludeInProgramGuide))
        {
            foreach (PlaylistItem bi in allItems)
            {
                bi.IncludeInProgramGuide = true;
            }
        }

        return allItems.Map(Mapper.ProjectToViewModel).ToList();
    }
}
