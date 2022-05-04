using ErsatzTV.Core;
using Lucene.Net.Analysis;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using Lucene.Net.Util;
using Query = Lucene.Net.Search.Query;

namespace ErsatzTV.Infrastructure.Search;

public class CustomQueryParser : QueryParser
{
    internal static readonly List<string> NumericFields = new()
    {
        SearchIndex.MinutesField,
        SearchIndex.HeightField,
        SearchIndex.WidthField,
        SearchIndex.SeasonNumberField,
        SearchIndex.EpisodeNumberField
    };

    public CustomQueryParser(LuceneVersion matchVersion, string f, Analyzer a) : base(matchVersion, f, a)
    {
    }

    protected internal CustomQueryParser(ICharStream stream) : base(stream)
    {
    }

    protected CustomQueryParser(QueryParserTokenManager tm) : base(tm)
    {
    }

    protected override Query GetFieldQuery(string field, string queryText, bool quoted)
    {
        if (field == "released_onthisday")
        {
            var todayString = DateTime.Today.ToString("*MMdd");
            return base.GetWildcardQuery("release_date", todayString);
        }

        if (NumericFields.Contains(field) && int.TryParse(queryText, out int val))
        {
            var bytesRef = new BytesRef();
            NumericUtils.Int32ToPrefixCoded(val, 0, bytesRef);
            return NewTermQuery(new Term(field, bytesRef));
        }

        return base.GetFieldQuery(field, queryText, quoted);
    }

    protected override Query GetFieldQuery(string field, string queryText, int slop)
    {
        if (field == "released_inthelast" && ParseStart(queryText, out DateTime start))
        {
            var todayString = DateTime.UtcNow.ToString("yyyyMMdd");
            var dateString = start.ToString("yyyyMMdd");

            return base.GetRangeQuery("release_date", dateString, todayString, true, true);
        }

        if (field == "released_notinthelast" && ParseStart(queryText, out DateTime finish))
        {
            var dateString = finish.ToString("yyyyMMdd");

            return base.GetRangeQuery("release_date", "00000000", dateString, false, false);
        }

        if (field == "added_inthelast" && ParseStart(queryText, out DateTime addedStart))
        {
            var todayString = DateTime.UtcNow.ToString("yyyyMMdd");
            var dateString = addedStart.ToString("yyyyMMdd");

            return base.GetRangeQuery("added_date", dateString, todayString, true, true);
        }

        if (field == "added_notinthelast" && ParseStart(queryText, out DateTime addedFinish))
        {
            var dateString = addedFinish.ToString("yyyyMMdd");

            return base.GetRangeQuery("added_date", "00000000", dateString, false, false);
        }

        return base.GetFieldQuery(field, queryText, slop);
    }

    protected override Query GetRangeQuery(
        string field,
        string part1,
        string part2,
        bool startInclusive,
        bool endInclusive)
    {
        if (NumericFields.Contains(field) && int.TryParse(part1, out int min) && int.TryParse(part2, out int max))
        {
            return NumericRangeQuery.NewInt32Range(field, 1, min, max, startInclusive, endInclusive);
        }

        return base.GetRangeQuery(field, part1, part2, startInclusive, endInclusive);
    }

    internal static bool ParseStart(string text, out DateTime start)
    {
        start = SystemTime.MinValueUtc;

        try
        {
            if (int.TryParse(text.Split(" ")[0], out int number))
            {
                if (text.Contains("day"))
                {
                    start = DateTime.Today.AddDays(number * -1);
                    return true;
                }

                if (text.Contains("week"))
                {
                    start = DateTime.Today.AddDays(number * -7);
                    return true;
                }

                if (text.Contains("month"))
                {
                    start = DateTime.Today.AddMonths(number * -1);
                    return true;
                }

                if (text.Contains("year"))
                {
                    start = DateTime.Today.AddYears(number * -1);
                    return true;
                }
            }
        }
        catch
        {
            // do nothing
        }

        return false;
    }
}
