using System;
using System.Collections.Generic;
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
    public class SearchIndex : ISearchIndex
    {
        private const LuceneVersion AppLuceneVersion = LuceneVersion.LUCENE_48;

        private const string IdField = "id";
        private const string TypeField = "type";
        private const string ArtistField = "artist";
        private const string TitleField = "title";
        private const string SortTitleField = "sort_title";
        private const string GenreField = "genre";
        private const string TagField = "tag";
        private const string PlotField = "plot";
        private const string LibraryNameField = "library_name";
        private const string TitleAndYearField = "title_and_year";
        private const string JumpLetterField = "jump_letter";
        private const string ReleaseDateField = "release_date";
        private const string StudioField = "studio";

        private const string MovieType = "movie";
        private const string ShowType = "show";
        private const string MusicVideoType = "music_video";

        private static bool _isRebuilding;

        private readonly ILocalFileSystem _localFileSystem;
        private readonly ILogger<SearchIndex> _logger;

        private readonly ISearchRepository _searchRepository;

        public SearchIndex(
            ILocalFileSystem localFileSystem,
            ISearchRepository searchRepository,
            ILogger<SearchIndex> logger)
        {
            _localFileSystem = localFileSystem;
            _searchRepository = searchRepository;
            _logger = logger;
        }

        public int Version => 2;

        public Task<bool> Initialize()
        {
            _localFileSystem.EnsureFolderExists(FileSystemLayout.SearchIndexFolder);
            return Task.FromResult(true);
        }

        public async Task<Unit> Rebuild(List<int> itemIds)
        {
            _isRebuilding = true;

            await Initialize();

            using var dir = FSDirectory.Open(FileSystemLayout.SearchIndexFolder);
            var analyzer = new StandardAnalyzer(AppLuceneVersion);
            var indexConfig = new IndexWriterConfig(AppLuceneVersion, analyzer) { OpenMode = OpenMode.CREATE };
            using var writer = new IndexWriter(dir, indexConfig);

            foreach (int id in itemIds)
            {
                Option<MediaItem> maybeMediaItem = await _searchRepository.GetItemToIndex(id);
                if (maybeMediaItem.IsSome)
                {
                    MediaItem mediaItem = maybeMediaItem.ValueUnsafe();
                    switch (mediaItem)
                    {
                        case Movie movie:
                            UpdateMovie(movie, writer);
                            break;
                        case Show show:
                            UpdateShow(show, writer);
                            break;
                        case MusicVideo musicVideo:
                            UpdateMusicVideo(musicVideo, writer);
                            break;
                    }
                }
            }

            _isRebuilding = false;

            return Unit.Default;
        }

        public Task<Unit> AddItems(List<MediaItem> items) => UpdateItems(items);

        public Task<Unit> UpdateItems(List<MediaItem> items)
        {
            using var dir = FSDirectory.Open(FileSystemLayout.SearchIndexFolder);
            var analyzer = new StandardAnalyzer(AppLuceneVersion);
            var indexConfig = new IndexWriterConfig(AppLuceneVersion, analyzer) { OpenMode = OpenMode.APPEND };
            using var writer = new IndexWriter(dir, indexConfig);

            foreach (MediaItem item in items)
            {
                switch (item)
                {
                    case Movie movie:
                        UpdateMovie(movie, writer);
                        break;
                    case Show show:
                        UpdateShow(show, writer);
                        break;
                    case MusicVideo musicVideo:
                        UpdateMusicVideo(musicVideo, writer);
                        break;
                }
            }

            return Task.FromResult(Unit.Default);
        }

        public Task<Unit> RemoveItems(List<int> ids)
        {
            using var dir = FSDirectory.Open(FileSystemLayout.SearchIndexFolder);
            var analyzer = new StandardAnalyzer(AppLuceneVersion);
            var indexConfig = new IndexWriterConfig(AppLuceneVersion, analyzer) { OpenMode = OpenMode.APPEND };
            using var writer = new IndexWriter(dir, indexConfig);

            foreach (int id in ids)
            {
                writer.DeleteDocuments(new Term(IdField, id.ToString()));
            }

            return Task.FromResult(Unit.Default);
        }

        public Task<SearchResult> Search(string searchQuery, int skip, int limit, string searchField = "")
        {
            if (_isRebuilding ||
                string.IsNullOrWhiteSpace(searchQuery.Replace("*", string.Empty).Replace("?", string.Empty)))
            {
                return new SearchResult(new List<SearchItem>(), 0).AsTask();
            }

            using var dir = FSDirectory.Open(FileSystemLayout.SearchIndexFolder);
            using var reader = DirectoryReader.Open(dir);
            var searcher = new IndexSearcher(reader);
            int hitsLimit = skip + limit;
            using var analyzer = new StandardAnalyzer(AppLuceneVersion);
            QueryParser parser = !string.IsNullOrWhiteSpace(searchField)
                ? new QueryParser(AppLuceneVersion, searchField, analyzer)
                : new MultiFieldQueryParser(AppLuceneVersion, new[] { TitleField }, analyzer);
            parser.AllowLeadingWildcard = true;
            Query query = ParseQuery(searchQuery, parser);
            var filter = new DuplicateFilter(TitleAndYearField);
            var sort = new Sort(new SortField(SortTitleField, SortFieldType.STRING));
            TopFieldDocs topDocs = searcher.Search(query, filter, hitsLimit, sort, true, true);
            IEnumerable<ScoreDoc> selectedHits = topDocs.ScoreDocs.Skip(skip).Take(limit);

            var searchResult = new SearchResult(
                selectedHits.Map(d => ProjectToSearchItem(searcher.Doc(d.Doc))).ToList(),
                topDocs.TotalHits);

            searchResult.PageMap = GetSearchPageMap(searcher, query, filter, sort, limit);

            return searchResult.AsTask();
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

        private void UpdateMovie(Movie movie, IndexWriter writer)
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
                        new StringField(TitleAndYearField, GetTitleAndYear(metadata), Field.Store.NO),
                        new StringField(JumpLetterField, GetJumpLetter(metadata), Field.Store.YES)
                    };

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

                    writer.UpdateDocument(new Term(IdField, movie.Id.ToString()), doc);
                }
                catch (Exception ex)
                {
                    metadata.Movie = null;
                    _logger.LogWarning(ex, "Error indexing movie with metadata {@Metadata}", metadata);
                }
            }
        }

        private void UpdateShow(Show show, IndexWriter writer)
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
                        new StringField(TitleAndYearField, GetTitleAndYear(metadata), Field.Store.NO),
                        new StringField(JumpLetterField, GetJumpLetter(metadata), Field.Store.YES)
                    };

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

                    writer.UpdateDocument(new Term(IdField, show.Id.ToString()), doc);
                }
                catch (Exception ex)
                {
                    metadata.Show = null;
                    _logger.LogWarning(ex, "Error indexing show with metadata {@Metadata}", metadata);
                }
            }
        }
        
        private void UpdateMusicVideo(MusicVideo musicVideo, IndexWriter writer)
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
                        new TextField(ArtistField, metadata.Artist, Field.Store.NO),
                        new TextField(TitleField, metadata.Title, Field.Store.NO),
                        new StringField(SortTitleField, metadata.SortTitle.ToLowerInvariant(), Field.Store.NO),
                        new TextField(LibraryNameField, musicVideo.LibraryPath.Library.Name, Field.Store.NO),
                        new StringField(TitleAndYearField, GetTitleAndYear(metadata), Field.Store.NO),
                        new StringField(JumpLetterField, GetJumpLetter(metadata), Field.Store.YES)
                    };

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

                    writer.UpdateDocument(new Term(IdField, musicVideo.Id.ToString()), doc);
                }
                catch (Exception ex)
                {
                    metadata.MusicVideo = null;
                    _logger.LogWarning(ex, "Error indexing music video with metadata {@Metadata}", metadata);
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
            $"{metadata.Title}_{metadata.Year}";

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
