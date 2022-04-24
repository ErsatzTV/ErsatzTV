﻿using ErsatzTV.Application.MediaCards;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Extensions;
using ErsatzTV.Core.Interfaces.Emby;
using ErsatzTV.Core.Interfaces.Jellyfin;
using ErsatzTV.Core.Interfaces.Plex;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Search;
using ErsatzTV.Core.Search;
using static ErsatzTV.Application.MediaCards.Mapper;

namespace ErsatzTV.Application.Search;

public class
    QuerySearchIndexEpisodesHandler : IRequestHandler<QuerySearchIndexEpisodes, TelevisionEpisodeCardResultsViewModel>
{
    private readonly IEmbyPathReplacementService _embyPathReplacementService;
    private readonly IJellyfinPathReplacementService _jellyfinPathReplacementService;
    private readonly IMediaSourceRepository _mediaSourceRepository;
    private readonly IPlexPathReplacementService _plexPathReplacementService;
    private readonly ISearchIndex _searchIndex;
    private readonly ITelevisionRepository _televisionRepository;

    public QuerySearchIndexEpisodesHandler(
        ISearchIndex searchIndex,
        ITelevisionRepository televisionRepository,
        IMediaSourceRepository mediaSourceRepository,
        IPlexPathReplacementService plexPathReplacementService,
        IJellyfinPathReplacementService jellyfinPathReplacementService,
        IEmbyPathReplacementService embyPathReplacementService)
    {
        _searchIndex = searchIndex;
        _televisionRepository = televisionRepository;
        _mediaSourceRepository = mediaSourceRepository;
        _plexPathReplacementService = plexPathReplacementService;
        _jellyfinPathReplacementService = jellyfinPathReplacementService;
        _embyPathReplacementService = embyPathReplacementService;
    }

    public async Task<TelevisionEpisodeCardResultsViewModel> Handle(
        QuerySearchIndexEpisodes request,
        CancellationToken cancellationToken)
    {
        SearchResult searchResult = await _searchIndex.Search(
            request.Query,
            (request.PageNumber - 1) * request.PageSize,
            request.PageSize);

        Option<JellyfinMediaSource> maybeJellyfin = await _mediaSourceRepository.GetAllJellyfin()
            .Map(list => list.HeadOrNone());

        Option<EmbyMediaSource> maybeEmby = await _mediaSourceRepository.GetAllEmby()
            .Map(list => list.HeadOrNone());

        List<EpisodeMetadata> episodes = await _televisionRepository
            .GetEpisodesForCards(searchResult.Items.Map(i => i.Id).ToList());

        var items = new List<TelevisionEpisodeCardViewModel>();

        foreach (EpisodeMetadata episodeMetadata in episodes)
        {
            string localPath = await episodeMetadata.Episode.GetLocalPath(
                _plexPathReplacementService,
                _jellyfinPathReplacementService,
                _embyPathReplacementService,
                false);

            items.Add(ProjectToViewModel(episodeMetadata, maybeJellyfin, maybeEmby, true, localPath));
        }

        return new TelevisionEpisodeCardResultsViewModel(searchResult.TotalCount, items, searchResult.PageMap);
    }
}
