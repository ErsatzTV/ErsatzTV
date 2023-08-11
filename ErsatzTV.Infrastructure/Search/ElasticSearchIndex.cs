using System.Globalization;
using Bugsnag;
using Elastic.Clients.Elasticsearch;
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
    private const string IndexName = "ersatztv";    

    private readonly ILogger<ElasticSearchIndex> _logger;
    private readonly List<CultureInfo> _cultureInfos;
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

    public Task<bool> IndexExists() => Task.FromResult(true);

    public int Version => 36;

    public async Task<bool> Initialize(ILocalFileSystem localFileSystem, IConfigElementRepository configElementRepository)
    {
        _client = new ElasticsearchClient(new Uri("http://localhost:9200"));
        ExistsResponse exists = await _client.Indices.ExistsAsync(IndexName);
        if (!exists.IsValidResponse)
        {
            CreateIndexResponse createResponse = await CreateIndex();
            return createResponse.IsValidResponse;
        }

        return true;
    }

    public async Task<Unit> Rebuild(ICachingSearchRepository searchRepository, IFallbackMetadataProvider fallbackMetadataProvider)
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

    private async Task<CreateIndexResponse> CreateIndex()
    {
        return await _client.Indices.CreateAsync<BaseSearchItem>(
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
                )));
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
                // case Show show:
                //     await UpdateShow(searchRepository, show);
                //     break;
                // case Season season:
                //     await UpdateSeason(searchRepository, season);
                //     break;
                // case Artist artist:
                //     await UpdateArtist(searchRepository, artist);
                //     break;
                // case MusicVideo musicVideo:
                //     await UpdateMusicVideo(searchRepository, musicVideo);
                //     break;
                // case Episode episode:
                //     await UpdateEpisode(searchRepository, fallbackMetadataProvider, episode);
                //     break;
                // case OtherVideo otherVideo:
                //     await UpdateOtherVideo(searchRepository, otherVideo);
                //     break;
                // case Song song:
                //     await UpdateSong(searchRepository, song);
                //     break;
            }
        }

        return Unit.Default;
    }

    public async Task<Unit> RemoveItems(IEnumerable<int> ids)
    {
        await _client.BulkAsync(descriptor => descriptor
            .Index(IndexName)
            .DeleteMany(ids.Map(id => new Id(id)))
        );
        
        return Unit.Default;
    }

    public async Task<SearchResult> Search(IClient client, string query, int skip, int limit, string searchField = "")
    {
        var items = new List<ElasticSearchItem>();
        var totalCount = 0;

        SearchResponse<ElasticSearchItem> response = await _client.SearchAsync<ElasticSearchItem>(
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
        
        // if (limit > 0)
        // {
            searchResult.PageMap = Option<SearchPageMap>.None;
            //; GetSearchPageMap(searcher, query, null, sort, limit);
        // }

        return searchResult;
    }

    public void Commit()
    {
        // do nothing
    }

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
            // case Show show:
            //     await UpdateShow(searchRepository, show);
            //     break;
            // case Season season:
            //     await UpdateSeason(searchRepository, season);
            //     break;
            // case Artist artist:
            //     await UpdateArtist(searchRepository, artist);
            //     break;
            // case MusicVideo musicVideo:
            //     await UpdateMusicVideo(searchRepository, musicVideo);
            //     break;
            // case Episode episode:
            //     await UpdateEpisode(searchRepository, fallbackMetadataProvider, episode);
            //     break;
            // case OtherVideo otherVideo:
            //     await UpdateOtherVideo(searchRepository, otherVideo);
            //     break;
            // case Song song:
            //     await UpdateSong(searchRepository, song);
            //     break;
        }
    }

    private async Task UpdateMovie(ISearchRepository searchRepository, Movie movie)
    {
        foreach (MovieMetadata metadata in movie.MovieMetadata.HeadOrNone())
        {
            try
            {
                var doc = new SearchMovie
                {
                    Id = movie.Id,
                    Type = SearchIndex.MovieType,
                    Title = metadata.Title,
                    SortTitle = metadata.SortTitle.ToLowerInvariant(),
                    LibraryName = movie.LibraryPath.Library.Name,
                    LibraryId = movie.LibraryPath.Library.Id,
                    TitleAndYear = SearchIndex.GetTitleAndYear(metadata),
                    JumpLetter = SearchIndex.GetJumpLetter(metadata),
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

    private static string GetReleaseDate(DateTime? metadataReleaseDate)
    {
        return metadataReleaseDate?.ToString("yyyyMMdd");
    }

    private static string GetAddedDate(DateTime metadataAddedDate)
    {
        return metadataAddedDate.ToString("yyyyMMdd");
    }

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

    private static void AddStatistics(BaseSearchItem doc, IEnumerable<MediaVersion> mediaVersions)
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
}
