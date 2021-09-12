using System;
using System.Collections.Generic;
using Lucene.Net.Analysis;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using Lucene.Net.Util;

namespace ErsatzTV.Infrastructure.Search
{
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

            return base.GetFieldQuery(field, queryText, slop);
        }
    }
}
