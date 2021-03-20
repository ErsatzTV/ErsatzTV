using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Search;
using ErsatzTV.Core.Search;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Query = Lucene.Net.Search.Query;

namespace ErsatzTV.Infrastructure.Search
{
    public class SearchIndex : ISearchIndex
    {
        private const LuceneVersion AppLuceneVersion = LuceneVersion.LUCENE_48;

        private const string IdField = "id";
        private const string TypeField = "type";
        private const string TitleField = "title";
        private const string SortTitleField = "sort_title";
        private const string GenreField = "genre";
        private const string TagField = "tag";
        private const string PlotField = "plot";
        private const string LibraryName = "library_name";

        private const string MovieType = "movie";
        private const string ShowType = "show";

        private readonly ILocalFileSystem _localFileSystem;

        private readonly string[] _searchFields = { TitleField, GenreField, TagField };

        public SearchIndex(ILocalFileSystem localFileSystem) => _localFileSystem = localFileSystem;

        public int Version => 2;

        public Task<bool> Initialize()
        {
            _localFileSystem.EnsureFolderExists(FileSystemLayout.SearchIndexFolder);
            return Task.FromResult(true);
        }

        public async Task<Unit> Rebuild(List<MediaItem> items)
        {
            await Initialize();

            using var dir = FSDirectory.Open(FileSystemLayout.SearchIndexFolder);
            var analyzer = new StandardAnalyzer(AppLuceneVersion);
            var indexConfig = new IndexWriterConfig(AppLuceneVersion, analyzer) { OpenMode = OpenMode.CREATE };
            using var writer = new IndexWriter(dir, indexConfig);

            UpdateMovies(items.OfType<Movie>(), writer);
            UpdateShows(items.OfType<Show>(), writer);

            return Unit.Default;
        }

        public Task<Unit> AddItems(List<MediaItem> items) => UpdateItems(items);

        public Task<Unit> UpdateItems(List<MediaItem> items)
        {
            using var dir = FSDirectory.Open(FileSystemLayout.SearchIndexFolder);
            var analyzer = new StandardAnalyzer(AppLuceneVersion);
            var indexConfig = new IndexWriterConfig(AppLuceneVersion, analyzer) { OpenMode = OpenMode.APPEND };
            using var writer = new IndexWriter(dir, indexConfig);

            UpdateMovies(items.OfType<Movie>(), writer);
            UpdateShows(items.OfType<Show>(), writer);

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
            if (string.IsNullOrWhiteSpace(searchQuery.Replace("*", string.Empty).Replace("?", string.Empty)))
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
                : new MultiFieldQueryParser(AppLuceneVersion, _searchFields, analyzer);
            Query query = ParseQuery(searchQuery, parser);
            var sortField = new SortField(SortTitleField, SortFieldType.STRING);
            TopFieldDocs topDocs = searcher.Search(query, null, hitsLimit, new Sort(sortField), true, true);
            IEnumerable<ScoreDoc> selectedHits = topDocs.ScoreDocs.Skip(skip).Take(limit);
            return new SearchResult(
                selectedHits.Map(d => ProjectToSearchItem(searcher.Doc(d.Doc))).ToList(),
                topDocs.TotalHits).AsTask();
        }

        private static void UpdateMovies(IEnumerable<Movie> movies, IndexWriter writer)
        {
            foreach (Movie movie in movies)
            {
                Option<MovieMetadata> maybeMetadata = movie.MovieMetadata.HeadOrNone();
                if (maybeMetadata.IsSome)
                {
                    MovieMetadata metadata = maybeMetadata.ValueUnsafe();

                    var doc = new Document
                    {
                        new StringField(IdField, movie.Id.ToString(), Field.Store.YES),
                        new StringField(TypeField, MovieType, Field.Store.NO),
                        new TextField(TitleField, metadata.Title, Field.Store.NO),
                        new StringField(SortTitleField, metadata.SortTitle, Field.Store.NO),
                        new TextField(LibraryName, movie.LibraryPath.Library.Name, Field.Store.NO)
                    };

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

                    writer.UpdateDocument(new Term(IdField, movie.Id.ToString()), doc);
                }
            }
        }

        private static void UpdateShows(IEnumerable<Show> shows, IndexWriter writer)
        {
            foreach (Show show in shows)
            {
                Option<ShowMetadata> maybeMetadata = show.ShowMetadata.HeadOrNone();
                if (maybeMetadata.IsSome)
                {
                    ShowMetadata metadata = maybeMetadata.ValueUnsafe();

                    var doc = new Document
                    {
                        new StringField(IdField, show.Id.ToString(), Field.Store.YES),
                        new StringField(TypeField, ShowType, Field.Store.NO),
                        new TextField(TitleField, metadata.Title, Field.Store.NO),
                        new StringField(SortTitleField, metadata.SortTitle, Field.Store.NO),
                        new TextField(LibraryName, show.LibraryPath.Library.Name, Field.Store.NO)
                    };

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

                    writer.UpdateDocument(new Term(IdField, show.Id.ToString()), doc);
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
    }
}
