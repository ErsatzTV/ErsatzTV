using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Search;
using ErsatzTV.Core.Search;
using LanguageExt;
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
using Query = Lucene.Net.Search.Query;

namespace ErsatzTV.Infrastructure.Search
{
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
        private const string ReleaseDateField = "release_date";
        private const string StudioField = "studio";
        private const string LanguageField = "language";
        private const string StyleField = "style";
        private const string MoodField = "mood";
        private const string ActorField = "actor";
        private const string ContentRatingField = "content_rating";
        private const string DirectorField = "director";
        private const string WriterField = "writer";
        private const string SeasonNumberField = "season_number";
        private const string EpisodeNumberField = "episode_number";
        
        private const string MovieType = "movie";
        private const string ShowType = "show";
        private const string ArtistType = "artist";
        private const string MusicVideoType = "music_video";
        private const string EpisodeType = "episode";
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

        public int Version => 13;

        public Task<bool> Initialize(ILocalFileSystem localFileSystem)
        {
            if (!_initialized)
            {
                localFileSystem.EnsureFolderExists(FileSystemLayout.SearchIndexFolder);

                _directory = FSDirectory.Open(FileSystemLayout.SearchIndexFolder);
                var analyzer = new StandardAnalyzer(AppLuceneVersion);
                var indexConfig = new IndexWriterConfig(AppLuceneVersion, analyzer)
                    { OpenMode = OpenMode.CREATE_OR_APPEND };
                _writer = new IndexWriter(_directory, indexConfig);
                _initialized = true;
            }

            return Task.FromResult(_initialized);
        }

        public async Task<Unit> Rebuild(ISearchRepository searchRepository, List<int> itemIds)
        {
            _writer.DeleteAll();

            foreach (int id in itemIds)
            {
                Option<MediaItem> maybeMediaItem = await searchRepository.GetItemToIndex(id);
                if (maybeMediaItem.IsSome)
                {
                    MediaItem mediaItem = maybeMediaItem.ValueUnsafe();
                    switch (mediaItem)
                    {
                        case Movie movie:
                            UpdateMovie(movie);
                            break;
                        case Show show:
                            await UpdateShow(searchRepository, show);
                            break;
                        case Artist artist:
                            await UpdateArtist(searchRepository, artist);
                            break;
                        case MusicVideo musicVideo:
                            UpdateMusicVideo(musicVideo);
                            break;
                        case Episode episode:
                            UpdateEpisode(episode);
                            break;
                    }
                }
            }

            _writer.Commit();
            return Unit.Default;
        }

        public Task<Unit> AddItems(ISearchRepository searchRepository, List<MediaItem> items) =>
            UpdateItems(searchRepository, items);

        public async Task<Unit> UpdateItems(ISearchRepository searchRepository, List<MediaItem> items)
        {
            foreach (MediaItem item in items)
            {
                switch (item)
                {
                    case Movie movie:
                        UpdateMovie(movie);
                        break;
                    case Show show:
                        await UpdateShow(searchRepository, show);
                        break;
                    case Artist artist:
                        await UpdateArtist(searchRepository, artist);
                        break;
                    case MusicVideo musicVideo:
                        UpdateMusicVideo(musicVideo);
                        break;
                    case Episode episode:
                        UpdateEpisode(episode);
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
                { ContentRatingField, new KeywordAnalyzer() }
            };
            using var analyzerWrapper = new PerFieldAnalyzerWrapper(analyzer, customAnalyzers);
            QueryParser parser = !string.IsNullOrWhiteSpace(searchField)
                ? new QueryParser(AppLuceneVersion, searchField, analyzerWrapper)
                : new MultiFieldQueryParser(AppLuceneVersion, new[] { TitleField }, analyzerWrapper);
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

        private void UpdateMovie(Movie movie)
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
                        new StringField(TypeField, MovieType, Field.Store.NO),
                        new TextField(TitleField, metadata.Title, Field.Store.NO),
                        new StringField(SortTitleField, metadata.SortTitle.ToLowerInvariant(), Field.Store.NO),
                        new TextField(LibraryNameField, movie.LibraryPath.Library.Name, Field.Store.NO),
                        new StringField(LibraryIdField, movie.LibraryPath.Library.Id.ToString(), Field.Store.NO),
                        new StringField(TitleAndYearField, GetTitleAndYear(metadata), Field.Store.NO),
                        new StringField(JumpLetterField, GetJumpLetter(metadata), Field.Store.YES)
                    };

                    AddLanguages(doc, movie.MediaVersions);

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

                    _writer.UpdateDocument(new Term(IdField, movie.Id.ToString()), doc);
                }
                catch (Exception ex)
                {
                    metadata.Movie = null;
                    _logger.LogWarning(ex, "Error indexing movie with metadata {@Metadata}", metadata);
                }
            }
        }

        private void AddLanguages(Document doc, List<MediaVersion> mediaVersions)
        {
            Option<MediaVersion> maybeVersion = mediaVersions.HeadOrNone();
            if (maybeVersion.IsSome)
            {
                MediaVersion version = maybeVersion.ValueUnsafe();
                foreach (CultureInfo cultureInfo in version.Streams
                    .Filter(ms => ms.MediaStreamKind == MediaStreamKind.Audio)
                    .Map(ms => ms.Language).Distinct()
                    .Filter(s => !string.IsNullOrWhiteSpace(s))
                    .Map(
                        l => _cultureInfos.Filter(
                            c => string.Equals(c.ThreeLetterISOLanguageName, l, StringComparison.OrdinalIgnoreCase)))
                    .Sequence()
                    .Flatten())
                {
                    doc.Add(new TextField(LanguageField, cultureInfo.EnglishName, Field.Store.NO));
                }
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
                        new StringField(TypeField, ShowType, Field.Store.NO),
                        new TextField(TitleField, metadata.Title, Field.Store.NO),
                        new StringField(SortTitleField, metadata.SortTitle.ToLowerInvariant(), Field.Store.NO),
                        new TextField(LibraryNameField, show.LibraryPath.Library.Name, Field.Store.NO),
                        new StringField(LibraryIdField, show.LibraryPath.Library.Id.ToString(), Field.Store.NO),
                        new StringField(TitleAndYearField, GetTitleAndYear(metadata), Field.Store.NO),
                        new StringField(JumpLetterField, GetJumpLetter(metadata), Field.Store.YES)
                    };

                    List<string> languages = await searchRepository.GetLanguagesForShow(show);
                    foreach (CultureInfo cultureInfo in languages
                        .Distinct()
                        .Filter(s => !string.IsNullOrWhiteSpace(s))
                        .Map(
                            l => _cultureInfos.Filter(
                                c => string.Equals(
                                    c.ThreeLetterISOLanguageName,
                                    l,
                                    StringComparison.OrdinalIgnoreCase)))
                        .Sequence()
                        .Flatten())
                    {
                        doc.Add(new TextField(LanguageField, cultureInfo.EnglishName, Field.Store.NO));
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

                    _writer.UpdateDocument(new Term(IdField, show.Id.ToString()), doc);
                }
                catch (Exception ex)
                {
                    metadata.Show = null;
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
                        new StringField(TypeField, ArtistType, Field.Store.NO),
                        new TextField(TitleField, metadata.Title, Field.Store.NO),
                        new StringField(SortTitleField, metadata.SortTitle.ToLowerInvariant(), Field.Store.NO),
                        new TextField(LibraryNameField, artist.LibraryPath.Library.Name, Field.Store.NO),
                        new StringField(LibraryIdField, artist.LibraryPath.Library.Id.ToString(), Field.Store.NO),
                        new StringField(TitleAndYearField, GetTitleAndYear(metadata), Field.Store.NO),
                        new StringField(JumpLetterField, GetJumpLetter(metadata), Field.Store.YES)
                    };

                    List<string> languages = await searchRepository.GetLanguagesForArtist(artist);
                    foreach (CultureInfo cultureInfo in languages
                        .Distinct()
                        .Filter(s => !string.IsNullOrWhiteSpace(s))
                        .Map(
                            l => _cultureInfos.Filter(
                                c => string.Equals(
                                    c.ThreeLetterISOLanguageName,
                                    l,
                                    StringComparison.OrdinalIgnoreCase)))
                        .Sequence()
                        .Flatten())
                    {
                        doc.Add(new TextField(LanguageField, cultureInfo.EnglishName, Field.Store.NO));
                    }

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

        private void UpdateMusicVideo(MusicVideo musicVideo)
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
                        new StringField(TypeField, MusicVideoType, Field.Store.NO),
                        new TextField(TitleField, metadata.Title, Field.Store.NO),
                        new StringField(SortTitleField, metadata.SortTitle.ToLowerInvariant(), Field.Store.NO),
                        new TextField(LibraryNameField, musicVideo.LibraryPath.Library.Name, Field.Store.NO),
                        new StringField(LibraryIdField, musicVideo.LibraryPath.Library.Id.ToString(), Field.Store.NO),
                        new StringField(TitleAndYearField, GetTitleAndYear(metadata), Field.Store.NO),
                        new StringField(JumpLetterField, GetJumpLetter(metadata), Field.Store.YES)
                    };

                    AddLanguages(doc, musicVideo.MediaVersions);

                    if (metadata.ReleaseDate.HasValue)
                    {
                        doc.Add(
                            new StringField(
                                ReleaseDateField,
                                metadata.ReleaseDate.Value.ToString("yyyyMMdd"),
                                Field.Store.NO));
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

                    _writer.UpdateDocument(new Term(IdField, musicVideo.Id.ToString()), doc);
                }
                catch (Exception ex)
                {
                    metadata.MusicVideo = null;
                    _logger.LogWarning(ex, "Error indexing music video with metadata {@Metadata}", metadata);
                }
            }
        }
        
        private void UpdateEpisode(Episode episode)
        {
            Option<EpisodeMetadata> maybeMetadata = episode.EpisodeMetadata.HeadOrNone();
            if (maybeMetadata.IsSome)
            {
                EpisodeMetadata metadata = maybeMetadata.ValueUnsafe();

                try
                {
                    var doc = new Document
                    {
                        new StringField(IdField, episode.Id.ToString(), Field.Store.YES),
                        new StringField(TypeField, EpisodeType, Field.Store.NO),
                        new TextField(TitleField, metadata.Title, Field.Store.NO),
                        new StringField(SortTitleField, metadata.SortTitle.ToLowerInvariant(), Field.Store.NO),
                        new TextField(LibraryNameField, episode.LibraryPath.Library.Name, Field.Store.NO),
                        new StringField(LibraryIdField, episode.LibraryPath.Library.Id.ToString(), Field.Store.NO),
                        // new StringField(TitleAndYearField, GetTitleAndYear(metadata), Field.Store.NO),
                        new StringField(JumpLetterField, GetJumpLetter(metadata), Field.Store.YES),
                        new StringField(SeasonNumberField, episode.Season.SeasonNumber.ToString(), Field.Store.NO),
                        new StringField(EpisodeNumberField, episode.EpisodeNumber.ToString(), Field.Store.NO),
                    };

                    AddLanguages(doc, episode.MediaVersions);

                    if (metadata.ReleaseDate.HasValue)
                    {
                        doc.Add(
                            new StringField(
                                ReleaseDateField,
                                metadata.ReleaseDate.Value.ToString("yyyyMMdd"),
                                Field.Store.NO));
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

                    _writer.UpdateDocument(new Term(IdField, episode.Id.ToString()), doc);
                }
                catch (Exception ex)
                {
                    metadata.Episode = null;
                    _logger.LogWarning(ex, "Error indexing episode with metadata {@Metadata}", metadata);
                }
            }
        }

        private SearchItem ProjectToSearchItem(Document doc) => new(Convert.ToInt32(doc.Get(IdField)));

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

        private static string GetTitleAndYear(Metadata metadata) =>
            $"{metadata.Title}_{metadata.Year}".ToLowerInvariant();

        private static string GetJumpLetter(Metadata metadata)
        {
            char c = metadata.SortTitle.ToLowerInvariant().Head();
            return c switch
            {
                (>= 'a' and <= 'z') => c.ToString(),
                _ => "#"
            };
        }
    }
}
