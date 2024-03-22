using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Miscellaneous;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.QueryParsers.Classic;
using Query = Lucene.Net.Search.Query;

namespace ErsatzTV.Infrastructure.Search;

public static class SearchQueryParser
{
    internal static Query ParseQuery(string query)
    {
        using var analyzer = new SimpleAnalyzer(LuceneSearchIndex.AppLuceneVersion);
        var customAnalyzers = new Dictionary<string, Analyzer>
        {
            { LuceneSearchIndex.IdField, new KeywordAnalyzer() },
            { LuceneSearchIndex.LibraryIdField, new KeywordAnalyzer() },
            { LuceneSearchIndex.LibraryFolderIdField, new KeywordAnalyzer() },
            { LuceneSearchIndex.TypeField, new KeywordAnalyzer() },
            { LuceneSearchIndex.TagField, new KeywordAnalyzer() },
            { LuceneSearchIndex.ShowTagField, new KeywordAnalyzer() },
            { LuceneSearchIndex.ContentRatingField, new KeywordAnalyzer() },
            { LuceneSearchIndex.ShowContentRatingField, new KeywordAnalyzer() },
            { LuceneSearchIndex.StateField, new KeywordAnalyzer() },
            { LuceneSearchIndex.PlotField, new StandardAnalyzer(LuceneSearchIndex.AppLuceneVersion) }
        };
        using var analyzerWrapper = new PerFieldAnalyzerWrapper(analyzer, customAnalyzers);
        QueryParser parser = new CustomMultiFieldQueryParser(
            LuceneSearchIndex.AppLuceneVersion,
            [LuceneSearchIndex.TitleField],
            analyzerWrapper);
        parser.AllowLeadingWildcard = true;
        Query result = ParseQuery(query, parser);

        Serilog.Log.Logger.Debug("Search query parsed from [{Query}] to [{ParsedQuery}]", query, result.ToString());

        return result;
    }

    private static Query ParseQuery(string searchQuery, QueryParser parser)
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
