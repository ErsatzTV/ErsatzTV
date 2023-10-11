using System.Globalization;
using ErsatzTV.Core;
using Lucene.Net.Analysis;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using Lucene.Net.Util;
using Query = Lucene.Net.Search.Query;

namespace ErsatzTV.Infrastructure.Search;

public class CustomMultiFieldQueryParser : MultiFieldQueryParser
{
    private static readonly List<string> NumericFields = new()
    {
        LuceneSearchIndex.MinutesField,
        LuceneSearchIndex.SecondsField,
        LuceneSearchIndex.HeightField,
        LuceneSearchIndex.WidthField,
        LuceneSearchIndex.SeasonNumberField,
        LuceneSearchIndex.EpisodeNumberField,
        LuceneSearchIndex.VideoBitDepthField
    };

    public CustomMultiFieldQueryParser(
        LuceneVersion matchVersion,
        string[] fields,
        Analyzer analyzer,
        IDictionary<string, float> boosts) : base(matchVersion, fields, analyzer, boosts)
    {
    }

    public CustomMultiFieldQueryParser(LuceneVersion matchVersion, string[] fields, Analyzer analyzer) : base(
        matchVersion,
        fields,
        analyzer)
    {
    }

    protected override Query GetFieldQuery(string field, string queryText, bool quoted)
    {
        if (field == "released_onthisday")
        {
            var todayString = DateTime.Today.ToString("*MMdd", CultureInfo.InvariantCulture);
            return base.GetWildcardQuery(LuceneSearchIndex.ReleaseDateField, todayString);
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
            var todayString = DateTime.UtcNow.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
            var dateString = start.ToString("yyyyMMdd", CultureInfo.InvariantCulture);

            return base.GetRangeQuery(LuceneSearchIndex.ReleaseDateField, dateString, todayString, true, true);
        }

        if (field == "released_notinthelast" && ParseStart(queryText, out DateTime finish))
        {
            var dateString = finish.ToString("yyyyMMdd", CultureInfo.InvariantCulture);

            return base.GetRangeQuery(LuceneSearchIndex.ReleaseDateField, "00000000", dateString, false, false);
        }

        if (field == "added_inthelast" && ParseStart(queryText, out DateTime addedStart))
        {
            var todayString = DateTime.UtcNow.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
            var dateString = addedStart.ToString("yyyyMMdd", CultureInfo.InvariantCulture);

            return base.GetRangeQuery(LuceneSearchIndex.AddedDateField, dateString, todayString, true, true);
        }

        if (field == "added_notinthelast" && ParseStart(queryText, out DateTime addedFinish))
        {
            var dateString = addedFinish.ToString("yyyyMMdd", CultureInfo.InvariantCulture);

            return base.GetRangeQuery(LuceneSearchIndex.AddedDateField, "00000000", dateString, false, false);
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
        if (NumericFields.Contains(field))
        {
            if (part1 is null or "*" && int.TryParse(part2, out int max1))
            {
                return NumericRangeQuery.NewInt32Range(field, null, max1, startInclusive, endInclusive);
            }

            if (int.TryParse(part1, out int min))
            {
                if (part2 is null or "*")
                {
                    return NumericRangeQuery.NewInt32Range(field, min, null, startInclusive, endInclusive);
                }

                if (int.TryParse(part2, out int max))
                {
                    return NumericRangeQuery.NewInt32Range(field, min, max, startInclusive, endInclusive);
                }
            }
        }

        return base.GetRangeQuery(field, part1, part2, startInclusive, endInclusive);
    }

    private static bool ParseStart(string text, out DateTime start)
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
