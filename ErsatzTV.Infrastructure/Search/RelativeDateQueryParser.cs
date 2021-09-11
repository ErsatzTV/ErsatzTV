using System;
using Lucene.Net.Analysis;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using Lucene.Net.Util;

namespace ErsatzTV.Infrastructure.Search
{
    public class RelativeDateQueryParser : QueryParser
    {
        public RelativeDateQueryParser(LuceneVersion matchVersion, string f, Analyzer a) : base(matchVersion, f, a)
        {
        }

        protected internal RelativeDateQueryParser(ICharStream stream) : base(stream)
        {
        }

        protected RelativeDateQueryParser(QueryParserTokenManager tm) : base(tm)
        {
        }

        protected override Query GetFieldQuery(string field, string queryText, int slop)
        {
            if (field == "released_inthelast" && ParseStart(queryText, out DateTime start))
            {
                var todayString = DateTime.Today.ToString("yyyyMMdd");
                var dateString = start.ToString("yyyyMMdd");

                return base.GetRangeQuery("release_date", dateString, todayString, true, true);
            }

            return base.GetFieldQuery(field, queryText, slop);
        }
        
        internal static bool ParseStart(string text, out DateTime start)
        {
            start = DateTime.MinValue;

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
}
