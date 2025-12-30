using Bugsnag;
using ErsatzTV.Application.MediaCards;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Extensions;
using ErsatzTV.Core.Interfaces.Emby;
using ErsatzTV.Core.Interfaces.Jellyfin;
using ErsatzTV.Core.Interfaces.Plex;
using ErsatzTV.Core.Interfaces.Search;
using ErsatzTV.Core.Search;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using ErsatzTV.Core;
using static ErsatzTV.Application.MediaCards.Mapper;

namespace ErsatzTV.Application.Search;

public class
    QuerySearchIndexMusicVideosHandler(
        IClient client,
        ISearchIndex searchIndex,
        IPlexPathReplacementService plexPathReplacementService,
        IJellyfinPathReplacementService jellyfinPathReplacementService,
        IEmbyPathReplacementService embyPathReplacementService,
        IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<QuerySearchIndexMusicVideos, MusicVideoCardResultsViewModel>
{
    public async Task<MusicVideoCardResultsViewModel> Handle(
        QuerySearchIndexMusicVideos request,
        CancellationToken cancellationToken)
    {
        int pageSize = PaginationOptions.NormalizePageSize(request.PageSize);

        SearchResult searchResult = await searchIndex.Search(
            client,
            request.Query,
            string.Empty,
            (request.PageNumber - 1) * pageSize,
            pageSize,
            cancellationToken);

        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var ids = searchResult.Items.Map(i => i.Id).ToHashSet();
        List<MusicVideoMetadata> musicVideos = await dbContext.MusicVideoMetadata
            .AsNoTracking()
            .Filter(mvm => ids.Contains(mvm.MusicVideoId))
            .Include(mvm => mvm.MusicVideo)
            .ThenInclude(mv => mv.Artist)
            .ThenInclude(a => a.ArtistMetadata)
            .Include(mvm => mvm.MusicVideo)
            .ThenInclude(e => e.MediaVersions)
            .ThenInclude(mv => mv.MediaFiles)
            .Include(mvm => mvm.Artwork)
            .OrderBy(mvm => mvm.SortTitle)
            .ToListAsync(cancellationToken);

        var items = new List<MusicVideoCardViewModel>();

        foreach (MusicVideoMetadata musicVideoMetadata in musicVideos)
        {
            string localPath = await musicVideoMetadata.MusicVideo.GetLocalPath(
                plexPathReplacementService,
                jellyfinPathReplacementService,
                embyPathReplacementService,
                cancellationToken,
                false);

            items.Add(ProjectToViewModel(musicVideoMetadata, localPath));
        }

        return new MusicVideoCardResultsViewModel(searchResult.TotalCount, items, searchResult.PageMap);
    }
}
