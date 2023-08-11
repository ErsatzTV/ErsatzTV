using System.Globalization;
using Bugsnag;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Aggregations;
using Elastic.Clients.Elasticsearch.IndexManagement;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Repositories.Caching;
using ErsatzTV.Core.Interfaces.Search;
using ErsatzTV.Core.Search;
using ErsatzTV.FFmpeg;
using ErsatzTV.FFmpeg.Format;
using ErsatzTV.Infrastructure.Search.Models;
using Microsoft.Extensions.Logging;
using ExistsResponse = Elastic.Clients.Elasticsearch.IndexManagement.ExistsResponse;
using MediaStream = ErsatzTV.Core.Domain.MediaStream;

namespace ErsatzTV.Infrastructure.Search;

public class ElasticSearchIndex : ISearchIndex
{
    public static Uri Uri;
    public static string IndexName;
    private readonly List<CultureInfo> _cultureInfos;

    private readonly ILogger<ElasticSearchIndex> _logger;
    private ElasticsearchClient _client;

    public ElasticSearchIndex(ILogger<ElasticSearchIndex> logger)
    {
        _logger = logger;
        _cultureInfos = CultureInfo.GetCultures(CultureTypes.NeutralCultures).ToList();
    }

    public void Dispose()
    {
        // do nothing
    }

    // not really used by elasticsearch
    public Task<bool> IndexExists() => Task.FromResult(false);

    public int Version => 36;

    public async Task<bool> Initialize(
        ILocalFileSystem localFileSystem,
        IConfigElementRepository configElementRepository)
    {
        _client = new ElasticsearchClient(Uri);
        ExistsResponse exists = await _client.Indices.ExistsAsync(IndexName);
        if (!exists.IsValidResponse)
        {
            CreateIndexResponse createResponse = await CreateIndex();
            return createResponse.IsValidResponse;
        }

        return true;
    }

    public async Task<Unit> Rebuild(
        ICachingSearchRepository searchRepository,
        IFallbackMetadataProvider fallbackMetadataProvider)
    {
        DeleteIndexResponse deleteResponse = await _client.Indices.DeleteAsync(IndexName);
        if (!deleteResponse.IsValidResponse)
        {
            return Unit.Default;
        }

        CreateIndexResponse createResponse = await CreateIndex();
        if (!createResponse.IsValidResponse)
        {
            return Unit.Default;
        }

        await foreach (MediaItem mediaItem in searchRepository.GetAllMediaItems())
        {
            await RebuildItem(searchRepository, fallbackMetadataProvider, mediaItem);
        }

        return Unit.Default;
    }

    public async Task<Unit> RebuildItems(
        ICachingSearchRepository searchRepository,
        IFallbackMetadataProvider fallbackMetadataProvider,
        IEnumerable<int> itemIds)
    {
        foreach (int id in itemIds)
        {
            foreach (MediaItem mediaItem in await searchRepository.GetItemToIndex(id))
            {
                await RebuildItem(searchRepository, fallbackMetadataProvider, mediaItem);
            }
        }

        return Unit.Default;
    }

    public async Task<Unit> UpdateItems(
        ICachingSearchRepository searchRepository,
        IFallbackMetadataProvider fallbackMetadataProvider,
        List<MediaItem> items)
    {
        foreach (MediaItem item in items)
        {
            switch (item)
            {
                case Movie movie:
                    await UpdateMovie(searchRepository, movie);
                    break;
                case Show show:
                    await UpdateShow(searchRepository, show);
                    break;
                case Season season:
                    await UpdateSeason(searchRepository, season);
                    break;
                case Artist artist:
                    await UpdateArtist(searchRepository, artist);
                    break;
                case MusicVideo musicVideo:
                    await UpdateMusicVideo(searchRepository, musicVideo);
                    break;
                case Episode episode:
                    await UpdateEpisode(searchRepository, fallbackMetadataProvider, episode);
                    break;
                case OtherVideo otherVideo:
                    await UpdateOtherVideo(searchRepository, otherVideo);
                    break;
                case Song song:
                    await UpdateSong(searchRepository, song);
                    break;
            }
        }

        return Unit.Default;
    }

    public async Task<Unit> RemoveItems(IEnumerable<int> ids)
    {
        await _client.BulkAsync(
            descriptor => descriptor
                .Index(IndexName)
                .DeleteMany(ids.Map(id => new Id(id)))
        );

        return Unit.Default;
    }

    public async Task<SearchResult> Search(IClient client, string query, int skip, int limit, string searchField = "")
    {
        var items = new List<MinimalElasticSearchItem>();
        var totalCount = 0;

        SearchResponse<MinimalElasticSearchItem> response = await _client.SearchAsync<MinimalElasticSearchItem>(
            s => s.Index(IndexName)
                .Sort(ss => ss.Field(f => f.SortTitle, fs => fs.Order(SortOrder.Asc)))
                .From(skip)
                .Size(limit)
                .QueryLuceneSyntax(query));
        if (response.IsValidResponse)
        {
            items.AddRange(response.Documents);
            totalCount = (int)response.Total;
        }

        var searchResult = new SearchResult(items.Map(i => new SearchItem(i.Type, i.Id)).ToList(), totalCount);

        if (limit > 0)
        {
            searchResult.PageMap = await GetSearchPageMap(query, limit);
        }

        return searchResult;
    }

    public void Commit()
    {
        // do nothing
    }

    private async Task<CreateIndexResponse> CreateIndex() =>
        await _client.Indices.CreateAsync<ElasticSearchItem>(
            IndexName,
            i => i.Mappings(
                m => m.Properties(
                    p => p
                        .Keyword(t => t.Type, t => t.Store())
                        .Text(t => t.Title, t => t.Store(false))
                        .Keyword(t => t.SortTitle, t => t.Store(false))
                        .Text(t => t.LibraryName, t => t.Store(false))
                        .Keyword(t => t.LibraryId, t => t.Store(false))
                        .Keyword(t => t.TitleAndYear, t => t.Store(false))
                        .Keyword(t => t.JumpLetter, t => t.Store())
                        .Keyword(t => t.State, t => t.Store(false))
                        .Text(t => t.MetadataKind, t => t.Store(false))
                        .Text(t => t.Language, t => t.Store(false))
                        .IntegerNumber(t => t.Height, t => t.Store(false))
                        .IntegerNumber(t => t.Width, t => t.Store(false))
                        .Keyword(t => t.VideoCodec, t => t.Store(false))
                        .IntegerNumber(t => t.VideoBitDepth, t => t.Store(false))
                        .Keyword(t => t.VideoDynamicRange, t => t.Store(false))
                        .Keyword(t => t.ContentRating, t => t.Store(false))
                        .Keyword(t => t.ReleaseDate, t => t.Store(false))
                        .Keyword(t => t.AddedDate, t => t.Store(false))
                        .Text(t => t.Plot, t => t.Store(false))
                        .Text(t => t.Genre, t => t.Store(false))
                        .Text(t => t.Tag, t => t.Store(false))
                        .Text(t => t.Studio, t => t.Store(false))
                        .Text(t => t.Actor, t => t.Store(false))
                        .Text(t => t.Director, t => t.Store(false))
                        .Text(t => t.Writer, t => t.Store(false))
                        .Keyword(t => t.TraktList, t => t.Store(false))
                        .IntegerNumber(t => t.SeasonNumber, t => t.Store(false))
                        .Text(t => t.ShowTitle, t => t.Store(false))
                        .Text(t => t.ShowGenre, t => t.Store(false))
                        .Text(t => t.ShowTag, t => t.Store(false))
                        .Text(t => t.Style, t => t.Store(false))
                        .Text(t => t.Mood, t => t.Store(false))
                        .Text(t => t.Album, t => t.Store(false))
                        .Text(t => t.Artist, t => t.Store(false))
                        .IntegerNumber(t => t.EpisodeNumber, t => t.Store(false))
                        .Text(t => t.AlbumArtist, t => t.Store(false))
                )));

    private async Task RebuildItem(
        ISearchRepository searchRepository,
        IFallbackMetadataProvider fallbackMetadataProvider,
        MediaItem mediaItem)
    {
        switch (mediaItem)
        {
            case Movie movie:
                await UpdateMovie(searchRepository, movie);
                break;
            case Show show:
                await UpdateShow(searchRepository, show);
                break;
            case Season season:
                await UpdateSeason(searchRepository, season);
                break;
            case Artist artist:
                await UpdateArtist(searchRepository, artist);
                break;
            case MusicVideo musicVideo:
                await UpdateMusicVideo(searchRepository, musicVideo);
                break;
            case Episode episode:
                await UpdateEpisode(searchRepository, fallbackMetadataProvider, episode);
                break;
            case OtherVideo otherVideo:
                await UpdateOtherVideo(searchRepository, otherVideo);
                break;
            case Song song:
                await UpdateSong(searchRepository, song);
                break;
        }
    }

    private async Task UpdateMovie(ISearchRepository searchRepository, Movie movie)
    {
        foreach (MovieMetadata metadata in movie.MovieMetadata.HeadOrNone())
        {
            try
            {
                var doc = new ElasticSearchItem
                {
                    Id = movie.Id,
                    Type = LuceneSearchIndex.MovieType,
                    Title = metadata.Title,
                    SortTitle = metadata.SortTitle.ToLowerInvariant(),
                    LibraryName = movie.LibraryPath.Library.Name,
                    LibraryId = movie.LibraryPath.Library.Id,
                    TitleAndYear = LuceneSearchIndex.GetTitleAndYear(metadata),
                    JumpLetter = LuceneSearchIndex.GetJumpLetter(metadata),
                    State = movie.State.ToString(),
                    MetadataKind = metadata.MetadataKind.ToString(),
                    Language = await GetLanguages(searchRepository, movie.MediaVersions),
                    ContentRating = GetContentRatings(metadata.ContentRating),
                    ReleaseDate = GetReleaseDate(metadata.ReleaseDate),
                    AddedDate = GetAddedDate(metadata.DateAdded),
                    Plot = metadata.Plot ?? string.Empty,
                    Genre = metadata.Genres.Map(g => g.Name).ToList(),
                    Tag = metadata.Tags.Map(t => t.Name).ToList(),
                    Studio = metadata.Studios.Map(s => s.Name).ToList(),
                    Actor = metadata.Actors.Map(a => a.Name).ToList(),
                    Director = metadata.Directors.Map(d => d.Name).ToList(),
                    Writer = metadata.Writers.Map(w => w.Name).ToList(),
                    TraktList = movie.TraktListItems.Map(t => t.TraktList.TraktId.ToString()).ToList()
                };

                AddStatistics(doc, movie.MediaVersions);

                foreach ((string key, List<string> value) in GetMetadataGuids(metadata))
                {
                    doc.AdditionalProperties.Add(key, value);
                }

                await _client.IndexAsync(doc, IndexName);
            }
            catch (Exception ex)
            {
                metadata.Movie = null;
                _logger.LogWarning(ex, "Error indexing movie with metadata {@Metadata}", metadata);
            }
        }
    }

    private async Task UpdateShow(ISearchRepository searchRepository, Show show)
    {
        foreach (ShowMetadata metadata in show.ShowMetadata.HeadOrNone())
        {
            try
            {
                var doc = new ElasticSearchItem
                {
                    Id = show.Id,
                    Type = LuceneSearchIndex.ShowType,
                    Title = metadata.Title,
                    SortTitle = metadata.SortTitle.ToLowerInvariant(),
                    LibraryName = show.LibraryPath.Library.Name,
                    LibraryId = show.LibraryPath.Library.Id,
                    TitleAndYear = LuceneSearchIndex.GetTitleAndYear(metadata),
                    JumpLetter = LuceneSearchIndex.GetJumpLetter(metadata),
                    State = show.State.ToString(),
                    MetadataKind = metadata.MetadataKind.ToString(),
                    Language = await GetLanguages(searchRepository, await searchRepository.GetLanguagesForShow(show)),
                    ContentRating = GetContentRatings(metadata.ContentRating),
                    ReleaseDate = GetReleaseDate(metadata.ReleaseDate),
                    AddedDate = GetAddedDate(metadata.DateAdded),
                    Plot = metadata.Plot ?? string.Empty,
                    Genre = metadata.Genres.Map(g => g.Name).ToList(),
                    Tag = metadata.Tags.Map(t => t.Name).ToList(),
                    Studio = metadata.Studios.Map(s => s.Name).ToList(),
                    Actor = metadata.Actors.Map(a => a.Name).ToList(),
                    TraktList = show.TraktListItems.Map(t => t.TraktList.TraktId.ToString()).ToList()
                };

                foreach ((string key, List<string> value) in GetMetadataGuids(metadata))
                {
                    doc.AdditionalProperties.Add(key, value);
                }

                await _client.IndexAsync(doc, IndexName);
            }
            catch (Exception ex)
            {
                metadata.Show = null;
                _logger.LogWarning(ex, "Error indexing show with metadata {@Metadata}", metadata);
            }
        }
    }

    private async Task UpdateSeason(ISearchRepository searchRepository, Season season)
    {
        foreach (SeasonMetadata metadata in season.SeasonMetadata.HeadOrNone())
        foreach (ShowMetadata showMetadata in season.Show.ShowMetadata.HeadOrNone())
        {
            try
            {
                var seasonTitle = $"{showMetadata.Title} - S{season.SeasonNumber}";
                string sortTitle = $"{showMetadata.SortTitle}_{season.SeasonNumber:0000}"
                    .ToLowerInvariant();
                string titleAndYear = $"{showMetadata.Title}_{showMetadata.Year}_{season.SeasonNumber}"
                    .ToLowerInvariant();

                var doc = new ElasticSearchItem
                {
                    Id = season.Id,
                    Type = LuceneSearchIndex.SeasonType,
                    Title = seasonTitle,
                    SortTitle = sortTitle,
                    LibraryName = season.LibraryPath.Library.Name,
                    LibraryId = season.LibraryPath.Library.Id,
                    TitleAndYear = titleAndYear,
                    JumpLetter = LuceneSearchIndex.GetJumpLetter(showMetadata),
                    State = season.State.ToString(),
                    SeasonNumber = season.SeasonNumber,
                    ShowTitle = showMetadata.Title,
                    ShowGenre = showMetadata.Genres.Map(g => g.Name).ToList(),
                    ShowTag = showMetadata.Tags.Map(t => t.Name).ToList(),
                    Language = await GetLanguages(
                        searchRepository,
                        await searchRepository.GetLanguagesForSeason(season)),
                    ContentRating = GetContentRatings(showMetadata.ContentRating),
                    ReleaseDate = GetReleaseDate(metadata.ReleaseDate),
                    AddedDate = GetAddedDate(metadata.DateAdded),
                    TraktList = season.TraktListItems.Map(t => t.TraktList.TraktId.ToString()).ToList(),
                    Tag = metadata.Tags.Map(a => a.Name).ToList()
                };

                foreach ((string key, List<string> value) in GetMetadataGuids(metadata))
                {
                    doc.AdditionalProperties.Add(key, value);
                }

                await _client.IndexAsync(doc, IndexName);
            }
            catch (Exception ex)
            {
                metadata.Season = null;
                _logger.LogWarning(ex, "Error indexing season with metadata {@Metadata}", metadata);
            }
        }
    }

    private async Task UpdateArtist(ISearchRepository searchRepository, Artist artist)
    {
        foreach (ArtistMetadata metadata in artist.ArtistMetadata.HeadOrNone())
        {
            try
            {
                var doc = new ElasticSearchItem
                {
                    Id = artist.Id,
                    Type = LuceneSearchIndex.ArtistType,
                    Title = metadata.Title,
                    SortTitle = metadata.SortTitle.ToLowerInvariant(),
                    LibraryName = artist.LibraryPath.Library.Name,
                    LibraryId = artist.LibraryPath.Library.Id,
                    TitleAndYear = LuceneSearchIndex.GetTitleAndYear(metadata),
                    JumpLetter = LuceneSearchIndex.GetJumpLetter(metadata),
                    State = artist.State.ToString(),
                    MetadataKind = metadata.MetadataKind.ToString(),
                    Language = await GetLanguages(
                        searchRepository,
                        await searchRepository.GetLanguagesForArtist(artist)),
                    AddedDate = GetAddedDate(metadata.DateAdded),
                    Genre = metadata.Genres.Map(g => g.Name).ToList(),
                    Style = metadata.Styles.Map(t => t.Name).ToList(),
                    Mood = metadata.Moods.Map(s => s.Name).ToList()
                };

                foreach ((string key, List<string> value) in GetMetadataGuids(metadata))
                {
                    doc.AdditionalProperties.Add(key, value);
                }

                await _client.IndexAsync(doc, IndexName);
            }
            catch (Exception ex)
            {
                metadata.Artist = null;
                _logger.LogWarning(ex, "Error indexing artist with metadata {@Metadata}", metadata);
            }
        }
    }

    private async Task UpdateMusicVideo(ISearchRepository searchRepository, MusicVideo musicVideo)
    {
        foreach (MusicVideoMetadata metadata in musicVideo.MusicVideoMetadata.HeadOrNone())
        {
            try
            {
                var doc = new ElasticSearchItem
                {
                    Id = musicVideo.Id,
                    Type = LuceneSearchIndex.MusicVideoType,
                    Title = metadata.Title,
                    SortTitle = metadata.SortTitle.ToLowerInvariant(),
                    LibraryName = musicVideo.LibraryPath.Library.Name,
                    LibraryId = musicVideo.LibraryPath.Library.Id,
                    TitleAndYear = LuceneSearchIndex.GetTitleAndYear(metadata),
                    JumpLetter = LuceneSearchIndex.GetJumpLetter(metadata),
                    State = musicVideo.State.ToString(),
                    MetadataKind = metadata.MetadataKind.ToString(),
                    Language = await GetLanguages(searchRepository, musicVideo.MediaVersions),
                    ReleaseDate = GetReleaseDate(metadata.ReleaseDate),
                    AddedDate = GetAddedDate(metadata.DateAdded),
                    Album = metadata.Album ?? string.Empty,
                    Plot = metadata.Plot ?? string.Empty,
                    Genre = metadata.Genres.Map(g => g.Name).ToList(),
                    Tag = metadata.Tags.Map(t => t.Name).ToList(),
                    Studio = metadata.Studios.Map(s => s.Name).ToList()
                };

                var artists = new System.Collections.Generic.HashSet<string>();

                if (musicVideo.Artist != null)
                {
                    foreach (ArtistMetadata artistMetadata in musicVideo.Artist.ArtistMetadata)
                    {
                        artists.Add(artistMetadata.Title);
                    }
                }

                foreach (MusicVideoArtist artist in metadata.Artists)
                {
                    artists.Add(artist.Name);
                }

                doc.Artist = artists.ToList();

                AddStatistics(doc, musicVideo.MediaVersions);

                foreach ((string key, List<string> value) in GetMetadataGuids(metadata))
                {
                    doc.AdditionalProperties.Add(key, value);
                }

                await _client.IndexAsync(doc, IndexName);
            }
            catch (Exception ex)
            {
                metadata.MusicVideo = null;
                _logger.LogWarning(ex, "Error indexing music video with metadata {@Metadata}", metadata);
            }
        }
    }

    private async Task UpdateEpisode(
        ISearchRepository searchRepository,
        IFallbackMetadataProvider fallbackMetadataProvider,
        Episode episode)
    {
        // try to load metadata here, since episodes without metadata won't index
        if (episode.EpisodeMetadata.Count == 0)
        {
            episode.EpisodeMetadata ??= new List<EpisodeMetadata>();
            episode.EpisodeMetadata = fallbackMetadataProvider.GetFallbackMetadata(episode);
            foreach (EpisodeMetadata metadata in episode.EpisodeMetadata)
            {
                metadata.Episode = episode;
            }
        }

        foreach (EpisodeMetadata metadata in episode.EpisodeMetadata.HeadOrNone())
        {
            try
            {
                var doc = new ElasticSearchItem
                {
                    Id = episode.Id,
                    Type = LuceneSearchIndex.EpisodeType,
                    Title = metadata.Title,
                    SortTitle = metadata.SortTitle.ToLowerInvariant(),
                    LibraryName = episode.LibraryPath.Library.Name,
                    LibraryId = episode.LibraryPath.Library.Id,
                    TitleAndYear = LuceneSearchIndex.GetTitleAndYear(metadata),
                    JumpLetter = LuceneSearchIndex.GetJumpLetter(metadata),
                    State = episode.State.ToString(),
                    MetadataKind = metadata.MetadataKind.ToString(),
                    SeasonNumber = episode.Season?.SeasonNumber ?? 0,
                    EpisodeNumber = metadata.EpisodeNumber,
                    Language = await GetLanguages(searchRepository, episode.MediaVersions),
                    ReleaseDate = GetReleaseDate(metadata.ReleaseDate),
                    AddedDate = GetAddedDate(metadata.DateAdded),
                    Plot = metadata.Plot ?? string.Empty,
                    Genre = metadata.Genres.Map(g => g.Name).ToList(),
                    Tag = metadata.Tags.Map(t => t.Name).ToList(),
                    Studio = metadata.Studios.Map(s => s.Name).ToList(),
                    Actor = metadata.Actors.Map(a => a.Name).ToList(),
                    Director = metadata.Directors.Map(d => d.Name).ToList(),
                    Writer = metadata.Writers.Map(w => w.Name).ToList(),
                    TraktList = episode.TraktListItems.Map(t => t.TraktList.TraktId.ToString()).ToList()
                };

                // add some show fields to help filter episodes within a particular show
                foreach (ShowMetadata showMetadata in Optional(episode.Season?.Show?.ShowMetadata).Flatten())
                {
                    doc.ShowTitle = showMetadata.Title;
                    doc.ShowGenre = showMetadata.Genres.Map(g => g.Name).ToList();
                    doc.ShowTag = showMetadata.Tags.Map(t => t.Name).ToList();
                }

                AddStatistics(doc, episode.MediaVersions);

                foreach ((string key, List<string> value) in GetMetadataGuids(metadata))
                {
                    doc.AdditionalProperties.Add(key, value);
                }

                await _client.IndexAsync(doc, IndexName);
            }
            catch (Exception ex)
            {
                metadata.Episode = null;
                _logger.LogWarning(ex, "Error indexing episode with metadata {@Metadata}", metadata);
            }
        }
    }

    private async Task UpdateOtherVideo(ISearchRepository searchRepository, OtherVideo otherVideo)
    {
        foreach (OtherVideoMetadata metadata in otherVideo.OtherVideoMetadata.HeadOrNone())
        {
            try
            {
                var doc = new ElasticSearchItem
                {
                    Id = otherVideo.Id,
                    Type = LuceneSearchIndex.OtherVideoType,
                    Title = metadata.Title,
                    SortTitle = metadata.SortTitle.ToLowerInvariant(),
                    LibraryName = otherVideo.LibraryPath.Library.Name,
                    LibraryId = otherVideo.LibraryPath.Library.Id,
                    TitleAndYear = LuceneSearchIndex.GetTitleAndYear(metadata),
                    JumpLetter = LuceneSearchIndex.GetJumpLetter(metadata),
                    State = otherVideo.State.ToString(),
                    MetadataKind = metadata.MetadataKind.ToString(),
                    Language = await GetLanguages(searchRepository, otherVideo.MediaVersions),
                    ContentRating = GetContentRatings(metadata.ContentRating),
                    ReleaseDate = GetReleaseDate(metadata.ReleaseDate),
                    AddedDate = GetAddedDate(metadata.DateAdded),
                    Plot = metadata.Plot ?? string.Empty,
                    Genre = metadata.Genres.Map(g => g.Name).ToList(),
                    Tag = metadata.Tags.Map(t => t.Name).ToList(),
                    Studio = metadata.Studios.Map(s => s.Name).ToList(),
                    Actor = metadata.Actors.Map(a => a.Name).ToList(),
                    Director = metadata.Directors.Map(d => d.Name).ToList(),
                    Writer = metadata.Writers.Map(w => w.Name).ToList()
                };

                AddStatistics(doc, otherVideo.MediaVersions);

                foreach ((string key, List<string> value) in GetMetadataGuids(metadata))
                {
                    doc.AdditionalProperties.Add(key, value);
                }

                await _client.IndexAsync(doc, IndexName);
            }
            catch (Exception ex)
            {
                metadata.OtherVideo = null;
                _logger.LogWarning(ex, "Error indexing other video with metadata {@Metadata}", metadata);
            }
        }
    }

    private async Task UpdateSong(ISearchRepository searchRepository, Song song)
    {
        foreach (SongMetadata metadata in song.SongMetadata.HeadOrNone())
        {
            try
            {
                var doc = new ElasticSearchItem
                {
                    Id = song.Id,
                    Type = LuceneSearchIndex.SongType,
                    Title = metadata.Title,
                    SortTitle = metadata.SortTitle.ToLowerInvariant(),
                    LibraryName = song.LibraryPath.Library.Name,
                    LibraryId = song.LibraryPath.Library.Id,
                    TitleAndYear = LuceneSearchIndex.GetTitleAndYear(metadata),
                    JumpLetter = LuceneSearchIndex.GetJumpLetter(metadata),
                    State = song.State.ToString(),
                    MetadataKind = metadata.MetadataKind.ToString(),
                    Language = await GetLanguages(searchRepository, song.MediaVersions),
                    AddedDate = GetAddedDate(metadata.DateAdded),
                    Album = metadata.Album ?? string.Empty,
                    Artist = !string.IsNullOrWhiteSpace(metadata.Artist) ? new List<string> { metadata.Artist } : null,
                    AlbumArtist = metadata.AlbumArtist,
                    Genre = metadata.Genres.Map(g => g.Name).ToList(),
                    Tag = metadata.Tags.Map(t => t.Name).ToList()
                };

                AddStatistics(doc, song.MediaVersions);

                foreach ((string key, List<string> value) in GetMetadataGuids(metadata))
                {
                    doc.AdditionalProperties.Add(key, value);
                }

                await _client.IndexAsync(doc, IndexName);
            }
            catch (Exception ex)
            {
                metadata.Song = null;
                _logger.LogWarning(ex, "Error indexing song with metadata {@Metadata}", metadata);
            }
        }
    }

    private static string GetReleaseDate(DateTime? metadataReleaseDate) => metadataReleaseDate?.ToString("yyyyMMdd");

    private static string GetAddedDate(DateTime metadataAddedDate) => metadataAddedDate.ToString("yyyyMMdd");

    private static List<string> GetContentRatings(string metadataContentRating)
    {
        var contentRatings = new List<string>();

        if (!string.IsNullOrWhiteSpace(metadataContentRating))
        {
            foreach (string contentRating in metadataContentRating.Split("/")
                         .Map(x => x.Trim()).Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                contentRatings.Add(contentRating);
            }
        }

        return contentRatings;
    }

    private async Task<List<string>> GetLanguages(
        ISearchRepository searchRepository,
        IEnumerable<MediaVersion> mediaVersions)
    {
        var result = new List<string>();

        foreach (MediaVersion version in mediaVersions.HeadOrNone())
        {
            var mediaCodes = version.Streams
                .Filter(ms => ms.MediaStreamKind == MediaStreamKind.Audio)
                .Map(ms => ms.Language)
                .Distinct()
                .ToList();

            result.AddRange(await GetLanguages(searchRepository, mediaCodes));
        }

        return result;
    }

    private async Task<List<string>> GetLanguages(ISearchRepository searchRepository, List<string> mediaCodes)
    {
        var englishNames = new System.Collections.Generic.HashSet<string>();
        foreach (string code in await searchRepository.GetAllLanguageCodes(mediaCodes))
        {
            Option<CultureInfo> maybeCultureInfo = _cultureInfos.Find(
                ci => string.Equals(ci.ThreeLetterISOLanguageName, code, StringComparison.OrdinalIgnoreCase));
            foreach (CultureInfo cultureInfo in maybeCultureInfo)
            {
                englishNames.Add(cultureInfo.EnglishName);
            }
        }

        return englishNames.ToList();
    }

    private static void AddStatistics(ElasticSearchItem doc, IEnumerable<MediaVersion> mediaVersions)
    {
        foreach (MediaVersion version in mediaVersions.HeadOrNone())
        {
            doc.Minutes = (int)Math.Ceiling(version.Duration.TotalMinutes);

            foreach (MediaStream videoStream in version.Streams
                         .Filter(s => s.MediaStreamKind == MediaStreamKind.Video)
                         .HeadOrNone())
            {
                doc.Height = version.Height;
                doc.Width = version.Width;

                if (!string.IsNullOrWhiteSpace(videoStream.Codec))
                {
                    doc.VideoCodec = videoStream.Codec;
                }

                Option<IPixelFormat> maybePixelFormat =
                    AvailablePixelFormats.ForPixelFormat(videoStream.PixelFormat, null);
                foreach (IPixelFormat pixelFormat in maybePixelFormat)
                {
                    doc.VideoBitDepth = pixelFormat.BitDepth;
                }

                var colorParams = new ColorParams(
                    videoStream.ColorRange,
                    videoStream.ColorSpace,
                    videoStream.ColorTransfer,
                    videoStream.ColorPrimaries);

                doc.VideoDynamicRange = colorParams.IsHdr ? "hdr" : "sdr";
            }
        }
    }

    private static Dictionary<string, List<string>> GetMetadataGuids(Metadata metadata)
    {
        var result = new Dictionary<string, List<string>>();

        foreach (MetadataGuid guid in metadata.Guids)
        {
            string[] split = (guid.Guid ?? string.Empty).Split("://");
            if (split.Length == 2 && !string.IsNullOrWhiteSpace(split[1]))
            {
                string key = split[0];
                string value = split[1].ToLowerInvariant();
                if (!result.ContainsKey(key))
                {
                    result.Add(key, new List<string>());
                }

                result[key].Add(value);
            }
        }

        return result;
    }

    private async Task<Option<SearchPageMap>> GetSearchPageMap(string query, int limit)
    {
        SearchResponse<MinimalElasticSearchItem> response = await _client.SearchAsync<MinimalElasticSearchItem>(
            s => s.Index(IndexName)
                .Size(0)
                .Sort(ss => ss.Field(f => f.SortTitle, fs => fs.Order(SortOrder.Asc)))
                .Aggregations(a => a.Terms("count", v => v.Field(i => i.JumpLetter).Size(30)))
                .QueryLuceneSyntax(query));

        if (!response.IsValidResponse)
        {
            return Option<SearchPageMap>.None;
        }

        var letters = new List<char>
        {
            '#', 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't',
            'u', 'v', 'w', 'x', 'y', 'z'
        };
        var map = letters.ToDictionary(letter => letter, _ => 0);

        if (response.Aggregations?.Values.Head() is StringTermsAggregate aggregate)
        {
            // start on page 1
            int total = limit;

            foreach (char letter in letters)
            {
                map[letter] = total / limit;

                Option<StringTermsBucket> maybeBucket = aggregate.Buckets.Find(b => b.Key == letter.ToString());
                total += maybeBucket.Sum(bucket => (int)bucket.DocCount);
            }
        }

        return new SearchPageMap(map);
    }
}
