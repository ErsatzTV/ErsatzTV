using System;
using System.Collections.Generic;
using Lucene.Net.Analysis;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using Lucene.Net.Util;

namespace ErsatzTV.Infrastructure.Search;

public class CustomMultiFieldQueryParser : MultiFieldQueryParser
{
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
            var todayString = DateTime.Today.ToString("*MMdd");
            return base.GetWildcardQuery("release_date", todayString);
        }
            
        if (field == "minutes" && int.TryParse(queryText, out int val))
        {
            var bytesRef = new BytesRef();
            NumericUtils.Int32ToPrefixCoded(val, 0, bytesRef);
            return NewTermQuery(new Term("minutes", bytesRef));
        }

        return base.GetFieldQuery(field, queryText, quoted);
    }

    protected override Query GetFieldQuery(string field, string queryText, int slop)
    {
        if (field == "released_inthelast" && CustomQueryParser.ParseStart(queryText, out DateTime start))
        {
            var todayString = DateTime.Today.ToString("yyyyMMdd");
            var dateString = start.ToString("yyyyMMdd");

            return base.GetRangeQuery("release_date", dateString, todayString, true, true);
        }
            
        if (field == "released_notinthelast" && CustomQueryParser.ParseStart(queryText, out DateTime finish))
        {
            var dateString = finish.ToString("yyyyMMdd");

            return base.GetRangeQuery("release_date", "00000000", dateString, false, false);
        }
            
        if (field == "added_inthelast" && CustomQueryParser.ParseStart(queryText, out DateTime addedStart))
        {
            var todayString = DateTime.Today.ToString("yyyyMMdd");
            var dateString = addedStart.ToString("yyyyMMdd");

            return base.GetRangeQuery("added_date", dateString, todayString, true, true);
        }
            
        if (field == "added_notinthelast" && CustomQueryParser.ParseStart(queryText, out DateTime addedFinish))
        {
            var dateString = addedFinish.ToString("yyyyMMdd");

            return base.GetRangeQuery("added_date", "00000000", dateString, false, false);
        }
            
        return base.GetFieldQuery(field, queryText, slop);
    }
        
    protected override Query GetRangeQuery(string field, string part1, string part2, bool startInclusive, bool endInclusive)
    {
        if (field == "minutes" && int.TryParse(part1, out int min) && int.TryParse(part2, out int max))
        {
            return NumericRangeQuery.NewInt32Range(field, min, max, startInclusive, endInclusive);
        }

        return base.GetRangeQuery(field, part1, part2, startInclusive, endInclusive);
    }
}