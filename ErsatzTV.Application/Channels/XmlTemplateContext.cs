using System.Net;
using Scriban;
using Scriban.Parsing;

namespace ErsatzTV.Application.Channels;

public class XmlTemplateContext : TemplateContext
{
    public override TemplateContext Write(SourceSpan span, object textAsObject)
        => base.Write(span, textAsObject is string text ? WebUtility.HtmlEncode(text) : textAsObject);

    public override ValueTask<TemplateContext> WriteAsync(SourceSpan span, object textAsObject)
        => base.WriteAsync(span, textAsObject is string text ? WebUtility.HtmlEncode(text) : textAsObject);
}
