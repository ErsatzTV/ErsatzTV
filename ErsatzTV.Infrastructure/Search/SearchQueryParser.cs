using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Miscellaneous;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using Query = Lucene.Net.Search.Query;

namespace ErsatzTV.Infrastructure.Search;

public static class SearchQueryParser
{
    static SearchQueryParser()
    {
        BooleanQuery.MaxClauseCount = 1024 * 4;
    }
    
    internal static Analyzer AnalyzerWrapper()
    {
        using var defaultAnalyzer = new CustomAnalyzer(LuceneSearchIndex.AppLuceneVersion);
        using var keywordAnalyzer = new KeywordAnalyzer();
        var customAnalyzers = new Dictionary<string, Analyzer>
        {
            // StringField should use KeywordAnalyzer
            { LuceneSearchIndex.IdField, keywordAnalyzer },
            { LuceneSearchIndex.TypeField, keywordAnalyzer },
            { LuceneSearchIndex.SortTitleField, keywordAnalyzer },
            { LuceneSearchIndex.LibraryIdField, keywordAnalyzer },
            { LuceneSearchIndex.TitleAndYearField, keywordAnalyzer },
            { LuceneSearchIndex.JumpLetterField, keywordAnalyzer },
            { LuceneSearchIndex.StateField, keywordAnalyzer },
            { LuceneSearchIndex.ContentRatingField, keywordAnalyzer },
            { LuceneSearchIndex.ReleaseDateField, keywordAnalyzer },
            { LuceneSearchIndex.AddedDateField, keywordAnalyzer },
            { LuceneSearchIndex.TraktListField, keywordAnalyzer },
            { LuceneSearchIndex.ShowContentRatingField, keywordAnalyzer },
            { LuceneSearchIndex.LibraryFolderIdField, keywordAnalyzer },
            { LuceneSearchIndex.VideoCodecField, keywordAnalyzer },
            { LuceneSearchIndex.VideoDynamicRange, keywordAnalyzer },

            { LuceneSearchIndex.PlotField, new StandardAnalyzer(LuceneSearchIndex.AppLuceneVersion) }
        };

        return new PerFieldAnalyzerWrapper(defaultAnalyzer, customAnalyzers);
    }

    public static Query ParseQuery(string query)
    {
        using Analyzer analyzerWrapper = AnalyzerWrapper();
        
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
