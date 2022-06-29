using System.Globalization;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Search;
using ErsatzTV.Core.Search;
using LanguageExt.UnsafeValueAccess;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Miscellaneous;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Sandbox.Queries;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Microsoft.Extensions.Logging;
using Directory = System.IO.Directory;
using Query = Lucene.Net.Search.Query;

namespace ErsatzTV.Infrastructure.Search;

public sealed class SearchIndex : ISearchIndex
{
    private const LuceneVersion AppLuceneVersion = LuceneVersion.LUCENE_48;

    private const string IdField = "id";
    private const string TypeField = "type";
    private const string TitleField = "title";
    private const string SortTitleField = "sort_title";
    private const string GenreField = "genre";
    private const string TagField = "tag";
    private const string PlotField = "plot";
    private const string LibraryNameField = "library_name";
    private const string LibraryIdField = "library_id";
    private const string TitleAndYearField = "title_and_year";
    private const string JumpLetterField = "jump_letter";
    private const string StudioField = "studio";
    private const string LanguageField = "language";
    private const string StyleField = "style";
    private const string MoodField = "mood";
    private const string ActorField = "actor";
    private const string ContentRatingField = "content_rating";
    private const string DirectorField = "director";
    private const string WriterField = "writer";
    private const string TraktListField = "trakt_list";
    private const string AlbumField = "album";
    private const string ArtistField = "artist";
    private const string StateField = "state";
    private const string AlbumArtistField = "album_artist";
    private const string ShowTitleField = "show_title";
    private const string ShowGenreField = "show_genre";
    private const string ShowTagField = "show_tag";
    private const string MetadataKindField = "metadata_kind";

    internal const string MinutesField = "minutes";
    internal const string HeightField = "height";
    internal const string WidthField = "width";
    internal const string SeasonNumberField = "season_number";
    internal const string EpisodeNumberField = "episode_number";
    internal const string AddedDateField = "added_date";
    internal const string ReleaseDateField = "release_date";

    public const string MovieType = "movie";
    public const string ShowType = "show";
    public const string SeasonType = "season";
    public const string ArtistType = "artist";
    public const string MusicVideoType = "music_video";
    public const string EpisodeType = "episode";
    public const string OtherVideoType = "other_video";
    public const string SongType = "song";

    private readonly List<CultureInfo> _cultureInfos;

    private readonly ILogger<SearchIndex> _logger;

    private FSDirectory _directory;
    private bool _initialized;
    private IndexWriter _writer;

    public SearchIndex(ILogger<SearchIndex> logger)
    {
        _logger = logger;
        _cultureInfos = CultureInfo.GetCultures(CultureTypes.NeutralCultures).ToList();
        _initialized = false;
    }

    public int Version => 28;

    public async Task<bool> Initialize(
        ILocalFileSystem localFileSystem,
        IConfigElementRepository configElementRepository)
    {
        if (!_initialized)
        {
            localFileSystem.EnsureFolderExists(FileSystemLayout.SearchIndexFolder);

            if (!ValidateDirectory(FileSystemLayout.SearchIndexFolder))
            {
                _logger.LogWarning("Search index failed to initialize; will delete and recreate");
                await configElementRepository.Upsert(ConfigElementKey.SearchIndexVersion, 0);
                Directory.Delete(FileSystemLayout.SearchIndexFolder, true);
                localFileSystem.EnsureFolderExists(FileSystemLayout.SearchIndexFolder);
            }

            _directory = FSDirectory.Open(FileSystemLayout.SearchIndexFolder);
            var analyzer = new StandardAnalyzer(AppLuceneVersion);
            var indexConfig = new IndexWriterConfig(AppLuceneVersion, analyzer)
                { OpenMode = OpenMode.CREATE_OR_APPEND };
            _writer = new IndexWriter(_directory, indexConfig);
            _initialized = true;
        }

        return _initialized;
    }

    public async Task<Unit> UpdateItems(
        ISearchRepository searchRepository,
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

    public Task<Unit> RemoveItems(List<int> ids)
    {
        foreach (int id in ids)
        {
            _writer.DeleteDocuments(new Term(IdField, id.ToString()));
        }

        return Task.FromResult(Unit.Default);
    }

    public Task<SearchResult> Search(string searchQuery, int skip, int limit, string searchField = "")
    {
        if (string.IsNullOrWhiteSpace(searchQuery.Replace("*", string.Empty).Replace("?", string.Empty)))
        {
            return new SearchResult(new List<SearchItem>(), 0).AsTask();
        }

        using DirectoryReader reader = _writer.GetReader(true);
        var searcher = new IndexSearcher(reader);
        int hitsLimit = limit == 0 ? searcher.IndexReader.MaxDoc : skip + limit;
        using var analyzer = new StandardAnalyzer(AppLuceneVersion);
        var customAnalyzers = new Dictionary<string, Analyzer>
        {
            { ContentRatingField, new KeywordAnalyzer() },
            { StateField, new KeywordAnalyzer() }
        };
        using var analyzerWrapper = new PerFieldAnalyzerWrapper(analyzer, customAnalyzers);
        QueryParser parser = !string.IsNullOrWhiteSpace(searchField)
            ? new CustomQueryParser(AppLuceneVersion, searchField, analyzerWrapper)
            : new CustomMultiFieldQueryParser(AppLuceneVersion, new[] { TitleField }, analyzerWrapper);
        parser.AllowLeadingWildcard = true;
        Query query = ParseQuery(searchQuery, parser);
        var filter = new DuplicateFilter(TitleAndYearField);
        var sort = new Sort(new SortField(SortTitleField, SortFieldType.STRING));
        TopFieldDocs topDocs = searcher.Search(query, filter, hitsLimit, sort, true, true);
        IEnumerable<ScoreDoc> selectedHits = topDocs.ScoreDocs.Skip(skip);

        if (limit > 0)
        {
            selectedHits = selectedHits.Take(limit);
        }

        var searchResult = new SearchResult(
            selectedHits.Map(d => ProjectToSearchItem(searcher.Doc(d.Doc))).ToList(),
            topDocs.TotalHits);

        if (limit > 0)
        {
            searchResult.PageMap = GetSearchPageMap(searcher, query, filter, sort, limit);
        }

        return searchResult.AsTask();
    }

    public void Commit() => _writer.Commit();

    public void Dispose()
    {
        _writer?.Dispose();
        _directory?.Dispose();
    }

    public async Task<Unit> Rebuild(
        ISearchRepository searchRepository,
        IFallbackMetadataProvider fallbackMetadataProvider)
    {
        _writer.DeleteAll();
        _writer.Commit();

        await foreach (MediaItem mediaItem in searchRepository.GetAllMediaItems())
        {
            await RebuildItem(searchRepository, fallbackMetadataProvider, mediaItem);
        }

        _writer.Commit();
        return Unit.Default;
    }

    public async Task<Unit> RebuildItems(
        ISearchRepository searchRepository,
        IFallbackMetadataProvider fallbackMetadataProvider,
        List<int> itemIds)
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

    private bool ValidateDirectory(string folder)
    {
        try
        {
            using (var d = FSDirectory.Open(folder))
            {
                using (var analyzer = new StandardAnalyzer(AppLuceneVersion))
                {
                    var indexConfig = new IndexWriterConfig(AppLuceneVersion, analyzer)
                        { OpenMode = OpenMode.CREATE_OR_APPEND };
                    using (var w = new IndexWriter(d, indexConfig))
                    {
                        using (DirectoryReader _ = w.GetReader(true))
                        {
                            return true;
                        }
                    }
                }
            }
        }
        catch
        {
            return false;
        }
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

    private static Option<SearchPageMap> GetSearchPageMap(
        IndexSearcher searcher,
        Query query,
        DuplicateFilter filter,
        Sort sort,
        int limit)
    {
        ScoreDoc[] allDocs = searcher.Search(query, filter, int.MaxValue, sort, true, true).ScoreDocs;
        var letters = new List<char>
        {
            '#', 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't',
            'u', 'v', 'w', 'x', 'y', 'z'
        };
        var map = letters.ToDictionary(letter => letter, _ => 0);

        var current = 0;
        var page = 0;
        while (current < allDocs.Length)
        {
            // walk up by page size (limit)
            page++;
            current += limit;
            if (current > allDocs.Length)
            {
                current = allDocs.Length;
            }

            char jumpLetter = searcher.Doc(allDocs[current - 1].Doc).Get(JumpLetterField).Head();
            foreach (char letter in letters.Where(l => letters.IndexOf(l) <= letters.IndexOf(jumpLetter)))
            {
                if (map[letter] == 0)
                {
                    map[letter] = page;
                }
            }
        }

        int max = map.Values.Max();
        foreach (char letter in letters.Where(letter => map[letter] == 0))
        {
            map[letter] = max;
        }

        return new SearchPageMap(map);
    }

    private async Task UpdateMovie(ISearchRepository searchRepository, Movie movie)
    {
        Option<MovieMetadata> maybeMetadata = movie.MovieMetadata.HeadOrNone();
        if (maybeMetadata.IsSome)
        {
            MovieMetadata metadata = maybeMetadata.ValueUnsafe();

            try
            {
                var doc = new Document
                {
                    new StringField(IdField, movie.Id.ToString(), Field.Store.YES),
                    new StringField(TypeField, MovieType, Field.Store.YES),
                    new TextField(TitleField, metadata.Title, Field.Store.NO),
                    new StringField(SortTitleField, metadata.SortTitle.ToLowerInvariant(), Field.Store.NO),
                    new TextField(LibraryNameField, movie.LibraryPath.Library.Name, Field.Store.NO),
                    new StringField(LibraryIdField, movie.LibraryPath.Library.Id.ToString(), Field.Store.NO),
                    new StringField(TitleAndYearField, GetTitleAndYear(metadata), Field.Store.NO),
                    new StringField(JumpLetterField, GetJumpLetter(metadata), Field.Store.YES),
                    new StringField(StateField, movie.State.ToString(), Field.Store.NO),
                    new TextField(MetadataKindField, metadata.MetadataKind.ToString(), Field.Store.NO)
                };

                await AddLanguages(searchRepository, doc, movie.MediaVersions);

                foreach (MediaVersion version in movie.MediaVersions.HeadOrNone())
                {
                    doc.Add(
                        new Int32Field(MinutesField, (int)Math.Ceiling(version.Duration.TotalMinutes), Field.Store.NO));

                    doc.Add(new Int32Field(HeightField, version.Height, Field.Store.NO));
                    doc.Add(new Int32Field(WidthField, version.Width, Field.Store.NO));
                }

                if (!string.IsNullOrWhiteSpace(metadata.ContentRating))
                {
                    foreach (string contentRating in (metadata.ContentRating ?? string.Empty).Split("/")
                             .Map(x => x.Trim()).Where(x => !string.IsNullOrWhiteSpace(x)))
                    {
                        doc.Add(new StringField(ContentRatingField, contentRating, Field.Store.NO));
                    }
                }

                if (metadata.ReleaseDate.HasValue)
                {
                    doc.Add(
                        new StringField(
                            ReleaseDateField,
                            metadata.ReleaseDate.Value.ToString("yyyyMMdd"),
                            Field.Store.NO));
                }

                doc.Add(new StringField(AddedDateField, metadata.DateAdded.ToString("yyyyMMdd"), Field.Store.NO));

                if (!string.IsNullOrWhiteSpace(metadata.Plot))
                {
                    doc.Add(new TextField(PlotField, metadata.Plot ?? string.Empty, Field.Store.NO));
                }

                foreach (Genre genre in metadata.Genres)
                {
                    doc.Add(new TextField(GenreField, genre.Name, Field.Store.NO));
                }

                foreach (Tag tag in metadata.Tags)
                {
                    doc.Add(new TextField(TagField, tag.Name, Field.Store.NO));
                }

                foreach (Studio studio in metadata.Studios)
                {
                    doc.Add(new TextField(StudioField, studio.Name, Field.Store.NO));
                }

                foreach (Actor actor in metadata.Actors)
                {
                    doc.Add(new TextField(ActorField, actor.Name, Field.Store.NO));
                }

                foreach (Director director in metadata.Directors)
                {
                    doc.Add(new TextField(DirectorField, director.Name, Field.Store.NO));
                }

                foreach (Writer writer in metadata.Writers)
                {
                    doc.Add(new TextField(WriterField, writer.Name, Field.Store.NO));
                }

                foreach (TraktListItem item in movie.TraktListItems)
                {
                    doc.Add(new StringField(TraktListField, item.TraktList.TraktId.ToString(), Field.Store.NO));
                }

                _writer.UpdateDocument(new Term(IdField, movie.Id.ToString()), doc);
            }
            catch (Exception ex)
            {
                metadata.Movie = null;
                _logger.LogWarning(ex, "Error indexing movie with metadata {@Metadata}", metadata);
            }
        }
    }

    private async Task AddLanguages(ISearchRepository searchRepository, Document doc, List<MediaVersion> mediaVersions)
    {
        Option<MediaVersion> maybeVersion = mediaVersions.HeadOrNone();
        if (maybeVersion.IsSome)
        {
            MediaVersion version = maybeVersion.ValueUnsafe();
            var mediaCodes = version.Streams
                .Filter(ms => ms.MediaStreamKind == MediaStreamKind.Audio)
                .Map(ms => ms.Language).Distinct()
                .ToList();

            await AddLanguages(searchRepository, doc, mediaCodes);
        }
    }

    private async Task AddLanguages(ISearchRepository searchRepository, Document doc, List<string> mediaCodes)
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

        foreach (string englishName in englishNames)
        {
            doc.Add(new TextField(LanguageField, englishName, Field.Store.NO));
        }
    }

    private async Task UpdateShow(ISearchRepository searchRepository, Show show)
    {
        Option<ShowMetadata> maybeMetadata = show.ShowMetadata.HeadOrNone();
        if (maybeMetadata.IsSome)
        {
            ShowMetadata metadata = maybeMetadata.ValueUnsafe();

            try
            {
                var doc = new Document
                {
                    new StringField(IdField, show.Id.ToString(), Field.Store.YES),
                    new StringField(TypeField, ShowType, Field.Store.YES),
                    new TextField(TitleField, metadata.Title, Field.Store.NO),
                    new StringField(SortTitleField, metadata.SortTitle.ToLowerInvariant(), Field.Store.NO),
                    new TextField(LibraryNameField, show.LibraryPath.Library.Name, Field.Store.NO),
                    new StringField(LibraryIdField, show.LibraryPath.Library.Id.ToString(), Field.Store.NO),
                    new StringField(TitleAndYearField, GetTitleAndYear(metadata), Field.Store.NO),
                    new StringField(JumpLetterField, GetJumpLetter(metadata), Field.Store.YES),
                    new StringField(StateField, show.State.ToString(), Field.Store.NO),
                    new TextField(MetadataKindField, metadata.MetadataKind.ToString(), Field.Store.NO)
                };

                List<string> languages = await searchRepository.GetLanguagesForShow(show);
                await AddLanguages(searchRepository, doc, languages);

                if (!string.IsNullOrWhiteSpace(metadata.ContentRating))
                {
                    foreach (string contentRating in (metadata.ContentRating ?? string.Empty).Split("/")
                             .Map(x => x.Trim()).Where(x => !string.IsNullOrWhiteSpace(x)))
                    {
                        doc.Add(new StringField(ContentRatingField, contentRating, Field.Store.NO));
                    }
                }

                if (metadata.ReleaseDate.HasValue)
                {
                    doc.Add(
                        new StringField(
                            ReleaseDateField,
                            metadata.ReleaseDate.Value.ToString("yyyyMMdd"),
                            Field.Store.NO));
                }

                doc.Add(new StringField(AddedDateField, metadata.DateAdded.ToString("yyyyMMdd"), Field.Store.NO));

                if (!string.IsNullOrWhiteSpace(metadata.Plot))
                {
                    doc.Add(new TextField(PlotField, metadata.Plot ?? string.Empty, Field.Store.NO));
                }

                foreach (Genre genre in metadata.Genres)
                {
                    doc.Add(new TextField(GenreField, genre.Name, Field.Store.NO));
                }

                foreach (Tag tag in metadata.Tags)
                {
                    doc.Add(new TextField(TagField, tag.Name, Field.Store.NO));
                }

                foreach (Studio studio in metadata.Studios)
                {
                    doc.Add(new TextField(StudioField, studio.Name, Field.Store.NO));
                }

                foreach (Actor actor in metadata.Actors)
                {
                    doc.Add(new TextField(ActorField, actor.Name, Field.Store.NO));
                }

                foreach (TraktListItem item in show.TraktListItems)
                {
                    doc.Add(new StringField(TraktListField, item.TraktList.TraktId.ToString(), Field.Store.NO));
                }

                _writer.UpdateDocument(new Term(IdField, show.Id.ToString()), doc);
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
        Option<SeasonMetadata> maybeMetadata = season.SeasonMetadata.HeadOrNone();
        Option<ShowMetadata> maybeShowMetadata = season.Show.ShowMetadata.HeadOrNone();
        if (maybeMetadata.IsSome && maybeShowMetadata.IsSome)
        {
            SeasonMetadata metadata = maybeMetadata.ValueUnsafe();
            ShowMetadata showMetadata = maybeShowMetadata.ValueUnsafe();

            try
            {
                string seasonTitle = $"{showMetadata.Title} - S{season.SeasonNumber}";
                string sortTitle = $"{showMetadata.SortTitle}_{season.SeasonNumber:0000}"
                    .ToLowerInvariant();
                string titleAndYear = $"{showMetadata.Title}_{showMetadata.Year}_{season.SeasonNumber}"
                    .ToLowerInvariant();

                var doc = new Document
                {
                    new StringField(IdField, season.Id.ToString(), Field.Store.YES),
                    new StringField(TypeField, SeasonType, Field.Store.YES),
                    new TextField(TitleField, seasonTitle, Field.Store.NO),
                    new StringField(SortTitleField, sortTitle, Field.Store.NO),
                    new TextField(LibraryNameField, season.LibraryPath.Library.Name, Field.Store.NO),
                    new StringField(LibraryIdField, season.LibraryPath.Library.Id.ToString(), Field.Store.NO),
                    new StringField(TitleAndYearField, titleAndYear, Field.Store.NO),
                    new StringField(JumpLetterField, GetJumpLetter(showMetadata), Field.Store.YES),
                    new StringField(StateField, season.State.ToString(), Field.Store.NO),
                    new Int32Field(SeasonNumberField, season.SeasonNumber, Field.Store.NO),
                    new TextField(ShowTitleField, showMetadata.Title, Field.Store.NO)
                };

                // add some show fields to help filter shows within a particular show
                foreach (Genre genre in showMetadata.Genres)
                {
                    doc.Add(new TextField(ShowGenreField, genre.Name, Field.Store.NO));
                }

                foreach (Tag tag in showMetadata.Tags)
                {
                    doc.Add(new TextField(ShowTagField, tag.Name, Field.Store.NO));
                }

                List<string> languages = await searchRepository.GetLanguagesForSeason(season);
                await AddLanguages(searchRepository, doc, languages);

                if (!string.IsNullOrWhiteSpace(showMetadata.ContentRating))
                {
                    foreach (string contentRating in (showMetadata.ContentRating ?? string.Empty).Split("/")
                             .Map(x => x.Trim()).Where(x => !string.IsNullOrWhiteSpace(x)))
                    {
                        doc.Add(new StringField(ContentRatingField, contentRating, Field.Store.NO));
                    }
                }

                if (metadata.ReleaseDate.HasValue)
                {
                    doc.Add(
                        new StringField(
                            ReleaseDateField,
                            metadata.ReleaseDate.Value.ToString("yyyyMMdd"),
                            Field.Store.NO));
                }

                doc.Add(new StringField(AddedDateField, metadata.DateAdded.ToString("yyyyMMdd"), Field.Store.NO));

                foreach (TraktListItem item in season.TraktListItems)
                {
                    doc.Add(new StringField(TraktListField, item.TraktList.TraktId.ToString(), Field.Store.NO));
                }

                foreach (Tag tag in metadata.Tags)
                {
                    doc.Add(new TextField(TagField, tag.Name, Field.Store.NO));
                }

                _writer.UpdateDocument(new Term(IdField, season.Id.ToString()), doc);
            }
            catch (Exception ex)
            {
                metadata.Season = null;
                _logger.LogWarning(ex, "Error indexing show with metadata {@Metadata}", metadata);
            }
        }
    }

    private async Task UpdateArtist(ISearchRepository searchRepository, Artist artist)
    {
        Option<ArtistMetadata> maybeMetadata = artist.ArtistMetadata.HeadOrNone();
        if (maybeMetadata.IsSome)
        {
            ArtistMetadata metadata = maybeMetadata.ValueUnsafe();

            try
            {
                var doc = new Document
                {
                    new StringField(IdField, artist.Id.ToString(), Field.Store.YES),
                    new StringField(TypeField, ArtistType, Field.Store.YES),
                    new TextField(TitleField, metadata.Title, Field.Store.NO),
                    new StringField(SortTitleField, metadata.SortTitle.ToLowerInvariant(), Field.Store.NO),
                    new TextField(LibraryNameField, artist.LibraryPath.Library.Name, Field.Store.NO),
                    new StringField(LibraryIdField, artist.LibraryPath.Library.Id.ToString(), Field.Store.NO),
                    new StringField(TitleAndYearField, GetTitleAndYear(metadata), Field.Store.NO),
                    new StringField(JumpLetterField, GetJumpLetter(metadata), Field.Store.YES),
                    new TextField(MetadataKindField, metadata.MetadataKind.ToString(), Field.Store.NO)
                };

                List<string> languages = await searchRepository.GetLanguagesForArtist(artist);
                await AddLanguages(searchRepository, doc, languages);

                doc.Add(new StringField(AddedDateField, metadata.DateAdded.ToString("yyyyMMdd"), Field.Store.NO));

                foreach (Genre genre in metadata.Genres)
                {
                    doc.Add(new TextField(GenreField, genre.Name, Field.Store.NO));
                }

                foreach (Style style in metadata.Styles)
                {
                    doc.Add(new TextField(StyleField, style.Name, Field.Store.NO));
                }

                foreach (Mood mood in metadata.Moods)
                {
                    doc.Add(new TextField(MoodField, mood.Name, Field.Store.NO));
                }

                _writer.UpdateDocument(new Term(IdField, artist.Id.ToString()), doc);
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
        Option<MusicVideoMetadata> maybeMetadata = musicVideo.MusicVideoMetadata.HeadOrNone();
        if (maybeMetadata.IsSome)
        {
            MusicVideoMetadata metadata = maybeMetadata.ValueUnsafe();

            try
            {
                var doc = new Document
                {
                    new StringField(IdField, musicVideo.Id.ToString(), Field.Store.YES),
                    new StringField(TypeField, MusicVideoType, Field.Store.YES),
                    new TextField(TitleField, metadata.Title, Field.Store.NO),
                    new StringField(SortTitleField, metadata.SortTitle.ToLowerInvariant(), Field.Store.NO),
                    new TextField(LibraryNameField, musicVideo.LibraryPath.Library.Name, Field.Store.NO),
                    new StringField(LibraryIdField, musicVideo.LibraryPath.Library.Id.ToString(), Field.Store.NO),
                    new StringField(TitleAndYearField, GetTitleAndYear(metadata), Field.Store.NO),
                    new StringField(JumpLetterField, GetJumpLetter(metadata), Field.Store.YES),
                    new StringField(StateField, musicVideo.State.ToString(), Field.Store.NO),
                    new TextField(MetadataKindField, metadata.MetadataKind.ToString(), Field.Store.NO)
                };

                await AddLanguages(searchRepository, doc, musicVideo.MediaVersions);

                foreach (MediaVersion version in musicVideo.MediaVersions.HeadOrNone())
                {
                    doc.Add(
                        new Int32Field(MinutesField, (int)Math.Ceiling(version.Duration.TotalMinutes), Field.Store.NO));

                    doc.Add(new Int32Field(HeightField, version.Height, Field.Store.NO));
                    doc.Add(new Int32Field(WidthField, version.Width, Field.Store.NO));
                }

                if (metadata.ReleaseDate.HasValue)
                {
                    doc.Add(
                        new StringField(
                            ReleaseDateField,
                            metadata.ReleaseDate.Value.ToString("yyyyMMdd"),
                            Field.Store.NO));
                }

                doc.Add(new StringField(AddedDateField, metadata.DateAdded.ToString("yyyyMMdd"), Field.Store.NO));

                if (!string.IsNullOrWhiteSpace(metadata.Album))
                {
                    doc.Add(new TextField(AlbumField, metadata.Album, Field.Store.NO));
                }

                if (!string.IsNullOrWhiteSpace(metadata.Plot))
                {
                    doc.Add(new TextField(PlotField, metadata.Plot ?? string.Empty, Field.Store.NO));
                }

                foreach (Genre genre in metadata.Genres)
                {
                    doc.Add(new TextField(GenreField, genre.Name, Field.Store.NO));
                }

                foreach (Tag tag in metadata.Tags)
                {
                    doc.Add(new TextField(TagField, tag.Name, Field.Store.NO));
                }

                foreach (Studio studio in metadata.Studios)
                {
                    doc.Add(new TextField(StudioField, studio.Name, Field.Store.NO));
                }

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

                foreach (string artist in artists)
                {
                    doc.Add(new TextField(ArtistField, artist, Field.Store.NO));
                }

                _writer.UpdateDocument(new Term(IdField, musicVideo.Id.ToString()), doc);
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

        foreach (EpisodeMetadata metadata in episode.EpisodeMetadata)
        {
            try
            {
                var doc = new Document
                {
                    new StringField(IdField, episode.Id.ToString(), Field.Store.YES),
                    new StringField(TypeField, EpisodeType, Field.Store.YES),
                    new TextField(LibraryNameField, episode.LibraryPath.Library.Name, Field.Store.NO),
                    new StringField(LibraryIdField, episode.LibraryPath.Library.Id.ToString(), Field.Store.NO),
                    new StringField(TitleAndYearField, GetTitleAndYear(metadata), Field.Store.NO),
                    new StringField(JumpLetterField, GetJumpLetter(metadata), Field.Store.YES),
                    new StringField(StateField, episode.State.ToString(), Field.Store.NO),
                    new Int32Field(SeasonNumberField, episode.Season?.SeasonNumber ?? 0, Field.Store.NO),
                    new Int32Field(EpisodeNumberField, metadata.EpisodeNumber, Field.Store.NO),
                    new TextField(MetadataKindField, metadata.MetadataKind.ToString(), Field.Store.NO)
                };

                // add some show fields to help filter episodes within a particular show
                foreach (ShowMetadata showMetadata in Optional(episode.Season?.Show?.ShowMetadata).Flatten())
                {
                    doc.Add(new TextField(ShowTitleField, showMetadata.Title, Field.Store.NO));

                    foreach (Genre genre in showMetadata.Genres)
                    {
                        doc.Add(new TextField(ShowGenreField, genre.Name, Field.Store.NO));
                    }

                    foreach (Tag tag in showMetadata.Tags)
                    {
                        doc.Add(new TextField(ShowTagField, tag.Name, Field.Store.NO));
                    }
                }

                if (!string.IsNullOrWhiteSpace(metadata.Title))
                {
                    doc.Add(new TextField(TitleField, metadata.Title, Field.Store.NO));
                }

                if (!string.IsNullOrWhiteSpace(metadata.SortTitle))
                {
                    doc.Add(new StringField(SortTitleField, metadata.SortTitle.ToLowerInvariant(), Field.Store.NO));
                }

                await AddLanguages(searchRepository, doc, episode.MediaVersions);

                foreach (MediaVersion version in episode.MediaVersions.HeadOrNone())
                {
                    doc.Add(
                        new Int32Field(MinutesField, (int)Math.Ceiling(version.Duration.TotalMinutes), Field.Store.NO));

                    doc.Add(new Int32Field(HeightField, version.Height, Field.Store.NO));
                    doc.Add(new Int32Field(WidthField, version.Width, Field.Store.NO));
                }

                if (metadata.ReleaseDate.HasValue)
                {
                    doc.Add(
                        new StringField(
                            ReleaseDateField,
                            metadata.ReleaseDate.Value.ToString("yyyyMMdd"),
                            Field.Store.NO));
                }

                doc.Add(new StringField(AddedDateField, metadata.DateAdded.ToString("yyyyMMdd"), Field.Store.NO));

                if (!string.IsNullOrWhiteSpace(metadata.Plot))
                {
                    doc.Add(new TextField(PlotField, metadata.Plot ?? string.Empty, Field.Store.NO));
                }

                foreach (Genre genre in metadata.Genres)
                {
                    doc.Add(new TextField(GenreField, genre.Name, Field.Store.NO));
                }

                foreach (Tag tag in metadata.Tags)
                {
                    doc.Add(new TextField(TagField, tag.Name, Field.Store.NO));
                }

                foreach (Studio studio in metadata.Studios)
                {
                    doc.Add(new TextField(StudioField, studio.Name, Field.Store.NO));
                }

                foreach (Actor actor in metadata.Actors)
                {
                    doc.Add(new TextField(ActorField, actor.Name, Field.Store.NO));
                }

                foreach (Director director in metadata.Directors)
                {
                    doc.Add(new TextField(DirectorField, director.Name, Field.Store.NO));
                }

                foreach (Writer writer in metadata.Writers)
                {
                    doc.Add(new TextField(WriterField, writer.Name, Field.Store.NO));
                }

                foreach (TraktListItem item in episode.TraktListItems)
                {
                    doc.Add(new StringField(TraktListField, item.TraktList.TraktId.ToString(), Field.Store.NO));
                }

                _writer.UpdateDocument(new Term(IdField, episode.Id.ToString()), doc);
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
        Option<OtherVideoMetadata> maybeMetadata = otherVideo.OtherVideoMetadata.HeadOrNone();
        if (maybeMetadata.IsSome)
        {
            OtherVideoMetadata metadata = maybeMetadata.ValueUnsafe();

            try
            {
                var doc = new Document
                {
                    new StringField(IdField, otherVideo.Id.ToString(), Field.Store.YES),
                    new StringField(TypeField, OtherVideoType, Field.Store.YES),
                    new TextField(TitleField, metadata.Title, Field.Store.NO),
                    new StringField(SortTitleField, metadata.SortTitle.ToLowerInvariant(), Field.Store.NO),
                    new TextField(LibraryNameField, otherVideo.LibraryPath.Library.Name, Field.Store.NO),
                    new StringField(LibraryIdField, otherVideo.LibraryPath.Library.Id.ToString(), Field.Store.NO),
                    new StringField(TitleAndYearField, GetTitleAndYear(metadata), Field.Store.NO),
                    new StringField(JumpLetterField, GetJumpLetter(metadata), Field.Store.YES),
                    new StringField(StateField, otherVideo.State.ToString(), Field.Store.NO),
                    new TextField(MetadataKindField, metadata.MetadataKind.ToString(), Field.Store.NO)
                };

                await AddLanguages(searchRepository, doc, otherVideo.MediaVersions);

                foreach (MediaVersion version in otherVideo.MediaVersions.HeadOrNone())
                {
                    doc.Add(
                        new Int32Field(MinutesField, (int)Math.Ceiling(version.Duration.TotalMinutes), Field.Store.NO));

                    doc.Add(new Int32Field(HeightField, version.Height, Field.Store.NO));
                    doc.Add(new Int32Field(WidthField, version.Width, Field.Store.NO));
                }

                if (!string.IsNullOrWhiteSpace(metadata.ContentRating))
                {
                    foreach (string contentRating in (metadata.ContentRating ?? string.Empty).Split("/")
                             .Map(x => x.Trim()).Where(x => !string.IsNullOrWhiteSpace(x)))
                    {
                        doc.Add(new StringField(ContentRatingField, contentRating, Field.Store.NO));
                    }
                }

                if (metadata.ReleaseDate.HasValue)
                {
                    doc.Add(
                        new StringField(
                            ReleaseDateField,
                            metadata.ReleaseDate.Value.ToString("yyyyMMdd"),
                            Field.Store.NO));
                }

                doc.Add(new StringField(AddedDateField, metadata.DateAdded.ToString("yyyyMMdd"), Field.Store.NO));

                if (!string.IsNullOrWhiteSpace(metadata.Plot))
                {
                    doc.Add(new TextField(PlotField, metadata.Plot ?? string.Empty, Field.Store.NO));
                }

                foreach (Genre genre in metadata.Genres)
                {
                    doc.Add(new TextField(GenreField, genre.Name, Field.Store.NO));
                }

                foreach (Tag tag in metadata.Tags)
                {
                    doc.Add(new TextField(TagField, tag.Name, Field.Store.NO));
                }

                foreach (Studio studio in metadata.Studios)
                {
                    doc.Add(new TextField(StudioField, studio.Name, Field.Store.NO));
                }

                foreach (Actor actor in metadata.Actors)
                {
                    doc.Add(new TextField(ActorField, actor.Name, Field.Store.NO));
                }

                foreach (Director director in metadata.Directors)
                {
                    doc.Add(new TextField(DirectorField, director.Name, Field.Store.NO));
                }

                foreach (Writer writer in metadata.Writers)
                {
                    doc.Add(new TextField(WriterField, writer.Name, Field.Store.NO));
                }

                _writer.UpdateDocument(new Term(IdField, otherVideo.Id.ToString()), doc);
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
        Option<SongMetadata> maybeMetadata = song.SongMetadata.HeadOrNone();
        if (maybeMetadata.IsSome)
        {
            SongMetadata metadata = maybeMetadata.ValueUnsafe();

            try
            {
                var doc = new Document
                {
                    new StringField(IdField, song.Id.ToString(), Field.Store.YES),
                    new StringField(TypeField, SongType, Field.Store.YES),
                    new TextField(TitleField, metadata.Title, Field.Store.NO),
                    new StringField(SortTitleField, metadata.SortTitle.ToLowerInvariant(), Field.Store.NO),
                    new TextField(LibraryNameField, song.LibraryPath.Library.Name, Field.Store.NO),
                    new StringField(LibraryIdField, song.LibraryPath.Library.Id.ToString(), Field.Store.NO),
                    new StringField(TitleAndYearField, GetTitleAndYear(metadata), Field.Store.NO),
                    new StringField(JumpLetterField, GetJumpLetter(metadata), Field.Store.YES),
                    new StringField(StateField, song.State.ToString(), Field.Store.NO),
                    new TextField(MetadataKindField, metadata.MetadataKind.ToString(), Field.Store.NO)
                };

                await AddLanguages(searchRepository, doc, song.MediaVersions);

                foreach (MediaVersion version in song.MediaVersions.HeadOrNone())
                {
                    doc.Add(
                        new Int32Field(MinutesField, (int)Math.Ceiling(version.Duration.TotalMinutes), Field.Store.NO));
                }

                doc.Add(new StringField(AddedDateField, metadata.DateAdded.ToString("yyyyMMdd"), Field.Store.NO));

                if (!string.IsNullOrWhiteSpace(metadata.Album))
                {
                    doc.Add(new TextField(AlbumField, metadata.Album, Field.Store.NO));
                }

                if (!string.IsNullOrWhiteSpace(metadata.Artist))
                {
                    doc.Add(new TextField(ArtistField, metadata.Artist, Field.Store.NO));
                }

                if (!string.IsNullOrWhiteSpace(metadata.AlbumArtist))
                {
                    doc.Add(new TextField(AlbumArtistField, metadata.AlbumArtist, Field.Store.NO));
                }

                foreach (Tag tag in metadata.Tags)
                {
                    doc.Add(new TextField(TagField, tag.Name, Field.Store.NO));
                }

                foreach (Genre genre in metadata.Genres)
                {
                    doc.Add(new TextField(GenreField, genre.Name, Field.Store.NO));
                }

                _writer.UpdateDocument(new Term(IdField, song.Id.ToString()), doc);
            }
            catch (Exception ex)
            {
                metadata.Song = null;
                _logger.LogWarning(ex, "Error indexing song with metadata {@Metadata}", metadata);
            }
        }
    }

    private SearchItem ProjectToSearchItem(Document doc) => new(
        doc.Get(TypeField),
        Convert.ToInt32(doc.Get(IdField)));

    private Query ParseQuery(string searchQuery, QueryParser parser)
    {
        Query query;
        try
        {
            query = parser.Parse(searchQuery.Trim());
        }
        catch (ParseException)
        {
            query = parser.Parse(QueryParserBase.Escape(searchQuery.Trim()));
        }

        return query;
    }

    // this is used for filtering duplicate search results
    private static string GetTitleAndYear(Metadata metadata) =>
        metadata switch
        {
            EpisodeMetadata em =>
                $"{em.Title}_{em.Year}_{em.Episode.Season.SeasonNumber}_{em.EpisodeNumber}_{em.Episode.State}"
                    .ToLowerInvariant(),
            OtherVideoMetadata ovm => $"{ovm.Title}_{ovm.Year}_{ovm.OtherVideo.State}".ToLowerInvariant(),
            SongMetadata sm => $"{sm.Title}_{sm.Year}_{sm.Song.State}".ToLowerInvariant(),
            MovieMetadata mm => $"{mm.Title}_{mm.Year}_{mm.Movie.State}".ToLowerInvariant(),
            ArtistMetadata am => $"{am.Title}_{am.Year}_{am.Artist.State}".ToLowerInvariant(),
            MusicVideoMetadata mvm => $"{mvm.Title}_{mvm.Year}_{mvm.MusicVideo.State}".ToLowerInvariant(),
            SeasonMetadata sm => $"{sm.Title}_{sm.Year}_{sm.Season.State}".ToLowerInvariant(),
            ShowMetadata sm => $"{sm.Title}_{sm.Year}_{sm.Show.State}".ToLowerInvariant(),
            _ => $"{metadata.Title}_{metadata.Year}".ToLowerInvariant()
        };

    private static string GetJumpLetter(Metadata metadata)
    {
        char c = (metadata.SortTitle ?? " ").ToLowerInvariant().Head();
        return c switch
        {
            (>= 'a' and <= 'z') => c.ToString(),
            _ => "#"
        };
    }
}
