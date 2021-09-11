using System;
using System.Collections.Generic;
using Lucene.Net.Analysis;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using Lucene.Net.Util;

namespace ErsatzTV.Infrastructure.Search
{
    public class RelativeDateMultiFieldQueryParser : MultiFieldQueryParser
    {
        public RelativeDateMultiFieldQueryParser(
            LuceneVersion matchVersion,
            string[] fields,
            Analyzer analyzer,
            IDictionary<string, float> boosts) : base(matchVersion, fields, analyzer, boosts)
        {
        }

        public RelativeDateMultiFieldQueryParser(LuceneVersion matchVersion, string[] fields, Analyzer analyzer) : base(
            matchVersion,
            fields,
            analyzer)
        {
        }

        protected override Query GetFieldQuery(string field, string queryText, int slop)
        {
            if (field == "released_inthelast" && RelativeDateQueryParser.ParseStart(queryText, out DateTime start))
            {
                var todayString = DateTime.Today.ToString("yyyyMMdd");
                var dateString = start.ToString("yyyyMMdd");

                return base.GetRangeQuery("release_date", dateString, todayString, true, true);
            }

            return base.GetFieldQuery(field, queryText, slop);
        }
    }
}
