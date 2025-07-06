using System.Text.RegularExpressions;
using ErsatzTV.Core.Search;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Miscellaneous;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using Serilog;
using Query = Lucene.Net.Search.Query;

namespace ErsatzTV.Infrastructure.Search;

public partial class SearchQueryParser(ISmartCollectionCache smartCollectionCache)
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

    public async Task<Query> ParseQuery(string query, string smartCollectionName)
    {
        string parsedQuery = query;

        if (!string.IsNullOrWhiteSpace(smartCollectionName) && await smartCollectionCache.HasCycle(smartCollectionName))
        {
            Log.Logger.Error("Smart collection {Name} contains a cycle; will not evaluate", smartCollectionName);
        }
        else
        {
            var replaceCount = 0;
            while (parsedQuery.Contains("smart_collection"))
            {
                if (replaceCount > 100)
                {
                    Log.Logger.Warning("smart_collection query is nested too deep; giving up");
                    break;
                }

                ReplaceResult replaceResult = await ReplaceSmartCollections(parsedQuery);
                if (replaceResult.Fatal)
                {
                    break;
                }

                if (parsedQuery == replaceResult.Query)
                {
                    Log.Logger.Warning(
                        "Failed to replace smart_collection in query; is the syntax correct? Quotes are required. Giving up...");
                    break;
                }

                parsedQuery = replaceResult.Query;

                replaceCount++;
            }
        }

        using Analyzer analyzerWrapper = AnalyzerWrapper();

        var parser = new CustomMultiFieldQueryParser(
            LuceneSearchIndex.AppLuceneVersion,
            [LuceneSearchIndex.TitleField],
            analyzerWrapper)
        {
            AllowLeadingWildcard = true
        };
        Query result = ParseQuery(parsedQuery, parser);

        Log.Logger.Debug("Search query parsed from [{Query}] to [{ParsedQuery}]", query, result.ToString());

        return result;
    }

    private async Task<ReplaceResult> ReplaceSmartCollections(string query)
    {
        try
        {
            string result = query;

            foreach (Match match in SmartCollectionRegex().Matches(query))
            {
                string smartCollectionName = match.Groups[1].Value;
                if (await smartCollectionCache.HasCycle(smartCollectionName))
                {
                    Log.Logger.Error(
                        "Smart collection {Name} contains a cycle; will not evaluate",
                        smartCollectionName);
                    return new ReplaceResult(query, true);
                }

                Option<string> maybeQuery = await smartCollectionCache.GetQuery(smartCollectionName);
                foreach (string smartCollectionQuery in maybeQuery)
                {
                    result = result.Replace(match.Value, $"({smartCollectionQuery})");
                }
            }

            return new ReplaceResult(result, false);
        }
        catch (Exception ex)
        {
            Log.Logger.Warning(ex, "Unexpected exception replacing smart collections in search query");
            return new ReplaceResult(query, true);
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

    [GeneratedRegex(
        """
        smart_collection:"([^"]+)"
        """)]
    internal static partial Regex SmartCollectionRegex();

    private record ReplaceResult(string Query, bool Fatal);
}
