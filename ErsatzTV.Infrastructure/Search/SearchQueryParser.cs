using System.Text.RegularExpressions;
using ErsatzTV.Infrastructure.Data;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Miscellaneous;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Query = Lucene.Net.Search.Query;

namespace ErsatzTV.Infrastructure.Search;

public partial class SearchQueryParser(IDbContextFactory<TvContext> dbContextFactory)
{
    static SearchQueryParser() => BooleanQuery.MaxClauseCount = 1024 * 4;

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
            { LuceneSearchIndex.VideoDynamicRangeField, keywordAnalyzer },
            { LuceneSearchIndex.TagFullField, keywordAnalyzer },
            { LuceneSearchIndex.CollectionField, keywordAnalyzer },

            { LuceneSearchIndex.PlotField, new StandardAnalyzer(LuceneSearchIndex.AppLuceneVersion) }
        };

        return new PerFieldAnalyzerWrapper(defaultAnalyzer, customAnalyzers);
    }

    public async Task<Query> ParseQuery(string query)
    {
        string parsedQuery = query;

        var replaceCount = 0;
        while (parsedQuery.Contains("smart_collection"))
        {
            if (replaceCount > 10)
            {
                Log.Logger.Warning("smart_collection query is nested too deep; giving up");
                break;
            }

            parsedQuery = await ReplaceSmartCollections(parsedQuery);

            replaceCount++;
        }

        using Analyzer analyzerWrapper = AnalyzerWrapper();

        QueryParser parser = new CustomMultiFieldQueryParser(
            LuceneSearchIndex.AppLuceneVersion,
            [LuceneSearchIndex.TitleField],
            analyzerWrapper);
        parser.AllowLeadingWildcard = true;
        Query result = ParseQuery(parsedQuery, parser);

        Log.Logger.Debug("Search query parsed from [{Query}] to [{ParsedQuery}]", query, result.ToString());

        return result;
    }

    private async Task<string> ReplaceSmartCollections(string query)
    {
        try
        {
            Regex regex = SmartCollectionRegex();
            await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync();
            Dictionary<string, string> smartCollectionMap = await dbContext.SmartCollections
                .ToDictionaryAsync(x => x.Name, x => x.Query, StringComparer.OrdinalIgnoreCase);
            return regex.Replace(query, match =>
            {
                string smartCollectionName = match.Groups[1].Value;
                return smartCollectionMap.TryGetValue(smartCollectionName, out string smartCollectionQuery)
                    ? $"({smartCollectionQuery})"
                    : match.Value;
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return query;
        }
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

    [GeneratedRegex("""
                    smart_collection:"([^"]+)"
                    """)]
    private static partial Regex SmartCollectionRegex();
}
