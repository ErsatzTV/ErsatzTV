using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Util;

namespace ErsatzTV.Infrastructure.Search;

public sealed class CustomAnalyzer(LuceneVersion matchVersion) : Analyzer
{
    protected override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
    {
        Tokenizer tokenizer = new WhitespaceTokenizer(matchVersion, reader);
        TokenStream result = new LowerCaseFilter(matchVersion, tokenizer);
        return new TokenStreamComponents(tokenizer, result);
    }
}
